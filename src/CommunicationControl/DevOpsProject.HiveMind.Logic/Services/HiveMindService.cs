using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DevOpsProject.HiveMind.Logic.State;
using System.Text;
using Polly;
using System.Linq;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class HiveMindService : IHiveMindService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HiveMindHttpClient _httpClient;
        private readonly ILogger<HiveMindService> _logger;
        private readonly HiveCommunicationConfig _communicationConfigurationOptions;
        private readonly IDroneRelayService _droneRelayService;
        private Timer _telemetryTimer;

        public HiveMindService(
            IHttpClientFactory httpClientFactory, 
            HiveMindHttpClient httpClient, 
            ILogger<HiveMindService> logger, 
            IOptionsSnapshot<HiveCommunicationConfig> communicationConfigurationOptions,
            IDroneRelayService droneRelayService)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _communicationConfigurationOptions = communicationConfigurationOptions.Value;
            _droneRelayService = droneRelayService;
        }

        public async Task ConnectHive(string? overrideHiveId = null)
        {
            var hiveId = ResolveHiveId(overrideHiveId);
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                throw new InvalidOperationException("Hive ID must be configured.");
            }

            HiveInMemoryState.SetHiveId(hiveId);

            var request = new HiveConnectRequest
            {
                HiveSchema = _communicationConfigurationOptions.RequestSchema,
                HiveIP = _communicationConfigurationOptions.HiveIP,
                HivePort = _communicationConfigurationOptions.HivePort,
                HiveID = hiveId
            };

            var httpClient = _httpClientFactory.CreateClient("HiveConnectClient");

            var uriBuilder = new UriBuilder
            {
                Scheme = _communicationConfigurationOptions.RequestSchema,
                Host = _communicationConfigurationOptions.CommunicationControlIP,
                Port = _communicationConfigurationOptions.CommunicationControlPort,
                Path = $"{_communicationConfigurationOptions.CommunicationControlPath}/connect"
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            _logger.LogInformation("Attempting to connect Hive {HiveId}. URI: {Uri}", hiveId, uriBuilder.Uri);

            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    10,
                    retryAttempt => TimeSpan.FromSeconds(2),
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Connecting HiveID: {HiveId}, retry attempt: {Retry}. Request URL: {Uri}", hiveId, retryCount, uriBuilder.Uri);
                    });

            var response = await retryPolicy.ExecuteAsync(() => httpClient.PostAsync(uriBuilder.Uri, jsonContent));

            if (response.IsSuccessStatusCode)
            {
                var connectResponse = await response.Content.ReadAsStringAsync();
                var hiveConnectResponse = JsonSerializer.Deserialize<HiveConnectResponse>(connectResponse);

                if (hiveConnectResponse != null && hiveConnectResponse.ConnectResult)
                {
                    HiveInMemoryState.OperationalArea = hiveConnectResponse.OperationalArea;
                    // Не встановлюємо телеметрію за замовчуванням - вона має бути 0/null, поки не задана вручну
                    // HiveInMemoryState.CurrentLocation = _communicationConfigurationOptions.InitialLocation;
                    // HiveInMemoryState.Height = 5.0f;
                    // HiveInMemoryState.Speed = 0.0f; // Вже 0 за замовчуванням
                    HiveInMemoryState.Interferences = hiveConnectResponse.Interferences;

                    StartTelemetry();
                }
                else
                {
                    _logger.LogInformation("Connecting hive failed for ID: {HiveId}", request.HiveID);
                    throw new Exception($"Failed to connect HiveID: {request.HiveID}");
                }
            }
            else
            {
                _logger.LogError("Failed to connect hive, terminating process");
                Environment.Exit(1);
            }
        }

        public bool AddInterference(InterferenceModel interferenceModel)
        {
            var isAdded = HiveInMemoryState.AddInterference(interferenceModel);
            return isAdded;
        }

        public void RemoveInterference(Guid interferenceId)
        {
            HiveInMemoryState.RemoveInterference(interferenceId);
        }

        public HiveTelemetryModel GetCurrentTelemetry()
        {
            var hiveId = HiveInMemoryState.GetHiveId() ?? _communicationConfigurationOptions.HiveID;
            var drones = new List<DroneTelemetryInfo>();

            // Збираємо дані про дронів та їх команди
            var allDrones = _droneRelayService.GetSwarm();
            var hiveDroneIds = HiveInMemoryState.GetHiveDrones(hiveId).ToHashSet();

            foreach (var drone in allDrones)
            {
                // Перевіряємо, чи дрон належить до поточного рою
                if (hiveDroneIds.Contains(drone.Id))
                {
                    var individualCommands = HiveInMemoryState.GetAllDroneCommands(drone.Id).ToList();
                    
                    // Перевіряємо, чи є команда для всього рою
                    // Команда для рою - це коли всі дрони мають хоча б одну команду одного типу
                    var hasHiveCommand = individualCommands.Any();

                    drones.Add(new DroneTelemetryInfo
                    {
                        DroneId = drone.Id,
                        Type = drone.Type,
                        ConnectionCount = drone.Connections?.Count ?? 0,
                        IndividualCommands = individualCommands,
                        HasHiveCommand = hasHiveCommand
                    });
                }
            }

            // Отримуємо телеметрію для поточного hive
            var location = HiveInMemoryState.CurrentLocation ?? new Location { Latitude = 0.0f, Longitude = 0.0f };
            
            return new HiveTelemetryModel
            {
                HiveID = hiveId,
                Location = location,
                Height = HiveInMemoryState.Height,
                Speed = HiveInMemoryState.Speed,
                State = HiveInMemoryState.IsMoving ? Shared.Enums.HiveMindState.Move : Shared.Enums.HiveMindState.Stop,
                Timestamp = DateTime.UtcNow,
                Drones = drones
            };
        }

        public HiveTelemetryModel? GetTelemetry(string hiveId)
        {
            // Перевіряємо, чи існує hive
            var hive = HiveInMemoryState.GetHive(hiveId);
            if (hive == null)
            {
                _logger.LogWarning("Cannot get telemetry: Hive with ID '{HiveId}' does not exist", hiveId);
                return null;
            }

            // Отримуємо телеметрію для конкретного hive
            var drones = new List<DroneTelemetryInfo>();
            var hiveDroneIds = HiveInMemoryState.GetHiveDrones(hiveId).ToHashSet();

            if (_droneRelayService != null)
            {
                var allDrones = _droneRelayService.GetSwarm();
                foreach (var drone in allDrones)
                {
                    if (hiveDroneIds.Contains(drone.Id))
                    {
                        var individualCommands = HiveInMemoryState.GetAllDroneCommands(drone.Id).ToList();
                        var hasHiveCommand = individualCommands.Any();

                        drones.Add(new DroneTelemetryInfo
                        {
                            DroneId = drone.Id,
                            Type = drone.Type,
                            ConnectionCount = drone.Connections?.Count ?? 0,
                            IndividualCommands = individualCommands,
                            HasHiveCommand = hasHiveCommand
                        });
                    }
                }
            }

            // Отримуємо телеметрію для конкретного hive без зміни поточного hive
            var telemetryData = HiveInMemoryState.GetHiveTelemetryData(hiveId);
            var location = telemetryData.CurrentLocation ?? new Location { Latitude = 0.0f, Longitude = 0.0f };
            
            return new HiveTelemetryModel
            {
                HiveID = hiveId,
                Location = location,
                Height = telemetryData.Height,
                Speed = telemetryData.Speed,
                State = telemetryData.IsMoving ? Shared.Enums.HiveMindState.Move : Shared.Enums.HiveMindState.Stop,
                Timestamp = DateTime.UtcNow,
                Drones = drones
            };
        }

        public async Task<bool> UpdateHiveIdentityAsync(string hiveId, bool reconnect)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                return false;
            }

            var normalized = hiveId.Trim();
            var existingHive = HiveInMemoryState.GetHive(normalized);
            if (existingHive == null)
            {
                _logger.LogWarning("Cannot update Hive identity to {HiveId} because this hive is not registered in HiveMind. Create the hive first.", normalized);
                return false;
            }

            var previousHiveId = HiveInMemoryState.GetHiveId();
            HiveInMemoryState.SetHiveId(normalized);
            _logger.LogInformation("Hive identity updated to {HiveId}", normalized);

            // При переключенні на інший hive телеметрія зберігається для кожного hive окремо
            // Не потрібно скидати телеметрію - вона автоматично завантажиться для нового hive

            if (reconnect)
            {
                StopTelemetry();
                await ConnectHive(normalized);
            }

            return true;
        }

        public bool UpdateTelemetry(string hiveId, Location? location, float? height, float? speed, bool? isMoving)
        {
            // Перевіряємо, чи існує hive
            var hive = HiveInMemoryState.GetHive(hiveId);
            if (hive == null)
            {
                _logger.LogWarning("Cannot update telemetry: Hive with ID '{HiveId}' does not exist", hiveId);
                return false;
            }

            bool updated = false;

            if (location.HasValue)
            {
                updated = true;
                _logger.LogInformation("Hive {HiveId} telemetry Location updated to Lat={Latitude:F6}, Lon={Longitude:F6}", 
                    hiveId, location.Value.Latitude, location.Value.Longitude);
            }

            if (height.HasValue)
            {
                updated = true;
                _logger.LogInformation("Hive {HiveId} telemetry Height updated to {Height:F2}m", hiveId, height.Value);
            }

            if (speed.HasValue)
            {
                updated = true;
                _logger.LogInformation("Hive {HiveId} telemetry Speed updated to {Speed:F2}m/s", hiveId, speed.Value);
            }

            if (isMoving.HasValue)
            {
                updated = true;
                _logger.LogInformation("Hive {HiveId} telemetry IsMoving updated to {IsMoving}", hiveId, isMoving.Value);
            }

            if (updated)
            {
                // Оновлюємо телеметрію для конкретного hive
                HiveInMemoryState.UpdateHiveTelemetry(hiveId, location, height, speed, isMoving);
            }

            return updated;
        }

        #region private methods
        private void StartTelemetry()
        {
            if (HiveInMemoryState.IsTelemetryRunning) return;
            // TODO: Sending telemetry each N seconds
            _telemetryTimer = new Timer(SendTelemetry, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            HiveInMemoryState.IsTelemetryRunning = true;

            _logger.LogInformation("Telemetry timer started.");
        }

        private void StopTelemetry()
        {
            _telemetryTimer?.Dispose();
            HiveInMemoryState.IsTelemetryRunning = false;

            _logger.LogInformation("Telemetry timer stopped.");
        }

        private async void SendTelemetry(object state)
        {
            var currentLocation = HiveInMemoryState.CurrentLocation;

            try
            {
                var telemetry = GetCurrentTelemetry();
                var request = new HiveTelemetryRequest
                {
                    HiveID = telemetry.HiveID,
                    Location = telemetry.Location,
                    Height = telemetry.Height,
                    Speed = telemetry.Speed,
                    State = telemetry.State,
                    Drones = telemetry.Drones
                };

                // Детальне логування телеметрії перед відправкою
                var droneCount = telemetry.Drones?.Count ?? 0;
                var dronesWithCommands = telemetry.Drones?.Count(d => d.IndividualCommands.Any()) ?? 0;
                var hiveCommandsCount = telemetry.Drones?.Count(d => d.HasHiveCommand) ?? 0;
                
                _logger.LogInformation(
                    "Sending Hive Telemetry | HiveID: {HiveID} | Location: Lat={Latitude:F6}, Lon={Longitude:F6} | Height: {Height:F2}m | Speed: {Speed:F2}m/s | State: {State} | Drones: {DroneCount} | Drones with commands: {DronesWithCommands} | Hive commands: {HiveCommandsCount} | Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
                    request.HiveID,
                    request.Location.Latitude,
                    request.Location.Longitude,
                    request.Height,
                    request.Speed,
                    request.State,
                    droneCount,
                    dronesWithCommands,
                    hiveCommandsCount,
                    telemetry.Timestamp);

                // Логування деталей про дронів з командами
                if (telemetry.Drones != null && telemetry.Drones.Any())
                {
                    foreach (var drone in telemetry.Drones.Where(d => d.IndividualCommands.Any()))
                    {
                        var commandTypes = string.Join(", ", drone.IndividualCommands.Select(c => c.CommandType.ToString()).Distinct());
                        _logger.LogInformation(
                            "  Drone {DroneId} ({Type}) | Connections: {ConnectionCount} | Commands: {CommandCount} | Types: [{CommandTypes}] | HiveCommand: {HasHiveCommand}",
                            drone.DroneId,
                            drone.Type,
                            drone.ConnectionCount,
                            drone.IndividualCommands.Count,
                            commandTypes,
                            drone.HasHiveCommand ? "Yes" : "No");
                    }
                }

                var connectResult = await _httpClient.SendCommunicationControlTelemetryAsync(_communicationConfigurationOptions.RequestSchema,
                    _communicationConfigurationOptions.CommunicationControlIP, _communicationConfigurationOptions.CommunicationControlPort,
                    _communicationConfigurationOptions.CommunicationControlPath, request);

                if (connectResult != null)
                {
                    var hiveConnectResponse = JsonSerializer.Deserialize<HiveTelemetryResponse>(connectResult);
                    _logger.LogInformation(
                        "Telemetry sent successfully | HiveID: {HiveID} | Response Timestamp: {ResponseTimestamp}",
                        request.HiveID,
                        hiveConnectResponse?.Timestamp ?? DateTime.UtcNow);
                }
                else
                {
                    _logger.LogError("Unable to send Hive telemetry for HiveID: {HiveID}", request.HiveID);
                    throw new Exception($"Failed to send telemetry for HiveID: {request.HiveID}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending telemetry: {Message}", ex.Message);
            }
        }

        private string? ResolveHiveId(string? overrideHiveId = null)
        {
            if (!string.IsNullOrWhiteSpace(overrideHiveId))
            {
                return overrideHiveId.Trim();
            }

            var stateId = HiveInMemoryState.GetHiveId();
            if (!string.IsNullOrWhiteSpace(stateId))
            {
                return stateId;
            }

            return _communicationConfigurationOptions.HiveID;
        }
        #endregion
    }
}
