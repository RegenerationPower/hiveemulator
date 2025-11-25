using DevOpsProject.Bms.Logic.Data;
using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DevOpsProject.Bms.Logic.Services;

public class CurrentStatusService : ICurrentStatusService
{
    private readonly BmsDbContext _db;

    public CurrentStatusService(BmsDbContext db)
    {
        _db = db;
    }

    public async Task UpsertHiveStatusAsync(HiveTelemetryModel telemetry, bool isInEwZone, CancellationToken ct = default)
    {
        var entity = await _db.HiveStatuses
            .FirstOrDefaultAsync(h => h.HiveId == telemetry.HiveID, ct);

        if (entity == null)
        {
            entity = new HiveStatus
            {
                HiveId = telemetry.HiveID
            };
            _db.HiveStatuses.Add(entity);
        }

        entity.Latitude  = telemetry.Location.Latitude;
        entity.Longitude = telemetry.Location.Longitude;
        entity.Height    = telemetry.Height;
        entity.Speed     = telemetry.Speed;
        entity.State     = telemetry.State;
        entity.IsInEwZone = isInEwZone;
        entity.LastTelemetryTimestampUtc = telemetry.Timestamp;

        await _db.SaveChangesAsync(ct);
    }

    public Task<List<HiveStatus>> GetAllStatusesAsync(CancellationToken ct = default)
        => _db.HiveStatuses.ToListAsync(ct);

    public Task<HiveStatus?> GetStatusAsync(string hiveId, CancellationToken ct = default)
        => _db.HiveStatuses.FirstOrDefaultAsync(h => h.HiveId == hiveId, ct);
}