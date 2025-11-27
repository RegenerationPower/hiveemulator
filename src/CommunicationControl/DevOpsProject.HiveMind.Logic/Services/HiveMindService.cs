using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DevOpsProject.HiveMind.Logic.State;
using System.Text;
using DevOpsProject.Shared.Models.DTO.hive;
using Polly;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class HiveMindService : IHiveMindService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HiveMindHttpClient _httpClient;
        private readonly ILogger<HiveMindService> _logger;
        private readonly HiveCommunicationConfig _communicationConfigurationOptions;
        private Timer _telemetryTimer;

        public HiveMindService(IHttpClientFactory httpClientFactory, HiveMindHttpClient httpClient, ILogger<HiveMindService> logger, IOptionsSnapshot<HiveCommunicationConfig> communicationConfigurationOptions)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _communicationConfigurationOptions = communicationConfigurationOptions.Value;
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
                    HiveInMemoryState.CurrentLocation = _communicationConfigurationOptions.InitialLocation;
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

        public void StopAllTelemetry()
        {
            StopTelemetry();
        }

        public HiveTelemetryModel GetCurrentTelemetry()
        {
            return new HiveTelemetryModel
            {
                HiveID = HiveInMemoryState.GetHiveId() ?? _communicationConfigurationOptions.HiveID,
                Location = HiveInMemoryState.CurrentLocation ?? default,
                Height = 5, // TODO: Get from actual state
                Speed = 15, // TODO: Get from actual state
                State = HiveInMemoryState.IsMoving ? Shared.Enums.HiveMindState.Move : Shared.Enums.HiveMindState.Stop,
                Timestamp = DateTime.UtcNow
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

            HiveInMemoryState.SetHiveId(normalized);
            _logger.LogInformation("Hive identity updated to {HiveId}", normalized);

            if (reconnect)
            {
                StopTelemetry();
                await ConnectHive(normalized);
            }

            return true;
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
                var request = new HiveTelemetryRequest
                {
                    HiveID = HiveInMemoryState.GetHiveId() ?? _communicationConfigurationOptions.HiveID,
                    Location = HiveInMemoryState.CurrentLocation ?? default,
                    // TODO: MOCKED FOR NOW
                    Height = 5,
                    Speed = 15,
                    State = Shared.Enums.HiveMindState.Move
                };

                var connectResult = await _httpClient.SendCommunicationControlTelemetryAsync(_communicationConfigurationOptions.RequestSchema,
                    _communicationConfigurationOptions.CommunicationControlIP, _communicationConfigurationOptions.CommunicationControlPort,
                    _communicationConfigurationOptions.CommunicationControlPath, request);

                _logger.LogInformation($"Telemetry sent for HiveID: {request.HiveID}: {connectResult}");

                if (connectResult != null)
                {
                    var hiveConnectResponse = JsonSerializer.Deserialize<HiveTelemetryResponse>(connectResult);
                }
                else
                {
                    _logger.LogError($"Unable to send Hive telemetry for HiveID: {request.HiveID}.");
                    throw new Exception($"Failed to send telemetry for HiveID: {request.HiveID}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending telemetry: {Message}", ex.Message);
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
