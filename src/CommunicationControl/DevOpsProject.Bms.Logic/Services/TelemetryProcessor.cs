using System.Text.Json;
using DevOpsProject.Bms.Logic.Data;
using DevOpsProject.Bms.Logic.Options;
using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Messages;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DevOpsProject.Bms.Logic.Services;

public class TelemetryProcessor : ITelemetryProcessor
    {
        private readonly BmsDbContext _db;
        private readonly ICurrentStatusService _currentStatus;
        private readonly IEwZoneService _ewZones;
        private readonly BmsMonitoringOptions _options;
        private readonly ILogger<TelemetryProcessor> _logger;

        public TelemetryProcessor(
            BmsDbContext db,
            ICurrentStatusService currentStatus,
            IEwZoneService ewZones,
            IOptions<BmsMonitoringOptions> options,
            ILogger<TelemetryProcessor> logger)
        {
            _db = db;
            _currentStatus = currentStatus;
            _ewZones = ewZones;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ProcessMessageAsync(RedisValue rawMessage, CancellationToken ct = default)
        {
            using var doc = JsonDocument.Parse((ReadOnlyMemory<byte>) rawMessage!);
            var root = doc.RootElement;

            if (!root.TryGetProperty("MessageType", out var typeProp))
            {
                _logger.LogWarning("Received message without MessageType: {message}", rawMessage.ToString());
                return;
            }

            var type = typeProp.GetString();

            switch (type)
            {
                case nameof(TelemetrySentMessage):
                {
                    var msg = JsonSerializer.Deserialize<TelemetrySentMessage>(rawMessage!);
                    if (msg?.Telemetry == null || !msg.IsSuccessfullySent)
                        return;

                    await HandleTelemetryAsync(msg.Telemetry, ct);
                    break;
                }

                case nameof(EwZoneAddedMessage):
                {
                    var msg = JsonSerializer.Deserialize<EwZoneAddedMessage>(rawMessage!);
                    if (msg?.Interference != null)
                        await _ewZones.UpsertZoneAsync(msg.Interference, ct);
                    break;
                }

                case nameof(EwZoneDeletedMessage):
                {
                    var msg = JsonSerializer.Deserialize<EwZoneDeletedMessage>(rawMessage!);
                    if (msg != null)
                        await _ewZones.DeactivateZoneAsync(msg.InterferenceId, ct);
                    break;
                }

                default:
                    // інші події (HiveConnected, HiveDisconnected) – ігноруємо або лог
                    _logger.LogDebug("Unhandled BMS message type: {type}", type);
                    break;
            }
        }

        private async Task HandleTelemetryAsync(HiveTelemetryModel telemetry, CancellationToken ct)
        {
            var activeZones = await _db.EwZones.Where(z => z.IsActive).ToListAsync(ct);

            bool isInZone = IsInsideAnyZone(telemetry.Location, activeZones);

            // оновити поточний стан Hive (BMS-2,3)
            await _currentStatus.UpsertHiveStatusAsync(telemetry, isInZone, ct);

            // записати історію телеметрії (опціонально)
            _db.TelemetryHistory.Add(new TelemetryHistory
            {
                HiveId        = telemetry.HiveID,
                Latitude      = telemetry.Location.Latitude,
                Longitude     = telemetry.Location.Longitude,
                Height        = telemetry.Height,
                Speed         = telemetry.Speed,
                State         = telemetry.State,
                IsInEwZone    = isInZone,
                TimestampUtc  = telemetry.Timestamp
            });

            await _db.SaveChangesAsync(ct);

            // Перевірка, чи не надто близько до інших Hive (частина моніторингу)
            await CheckHiveDistancesAsync(telemetry, ct);
        }

        private bool IsInsideAnyZone(Location location, IEnumerable<EwZone> zones)
        {
            foreach (var z in zones)
            {
                var dxDeg = location.Latitude  - z.CenterLatitude;
                var dyDeg = location.Longitude - z.CenterLongitude;

                var distanceKm =
                    Math.Sqrt(dxDeg * dxDeg + dyDeg * dyDeg) * _options.KmPerDegree;

                if (distanceKm <= z.RadiusKm)
                    return true;
            }

            return false;
        }

        private async Task CheckHiveDistancesAsync(HiveTelemetryModel telemetry, CancellationToken ct)
        {
            var allOther = await _db.HiveStatuses
                .Where(h => h.HiveId != telemetry.HiveID)
                .ToListAsync(ct);

            foreach (var other in allOther)
            {
                var dxDeg = telemetry.Location.Latitude  - other.Latitude;
                var dyDeg = telemetry.Location.Longitude - other.Longitude;

                var distanceKm =
                    Math.Sqrt(dxDeg * dxDeg + dyDeg * dyDeg) * _options.KmPerDegree;

                if (distanceKm <= _options.MinDistanceBetweenHivesKm)
                {
                    // Генеруємо рекомендацію на переміщення одного з Hive
                    var suggestion = new HiveRepositionSuggestion
                    {
                        SourceHiveId      = telemetry.HiveID,
                        OtherHiveId       = other.HiveId,
                        SourceLatitude    = telemetry.Location.Latitude,
                        SourceLongitude   = telemetry.Location.Longitude,
                        OtherLatitude     = other.Latitude,
                        OtherLongitude    = other.Longitude,
                        DistanceKm        = distanceKm,
                        SuggestedAtUtc    = DateTime.UtcNow,
                        IsConsumed        = false
                    };

                    _db.HiveRepositionSuggestions.Add(suggestion);
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }