using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.DTO.Drone;
using DevOpsProject.Shared.Models.DTO.Hive;
using DevOpsProject.Shared.Models.DTO.Topology;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DevOpsProject.CommunicationControl.Logic.Services
{
    public class HiveMindMeshIntegrationService : IHiveMindMeshIntegrationService
    {
        private const string HiveMindClientName = "HiveMindIntegrationClient";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<ComControlCommunicationConfiguration> _configuration;
        private readonly ILogger<HiveMindMeshIntegrationService> _logger;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public HiveMindMeshIntegrationService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ComControlCommunicationConfiguration> configuration,
            ILogger<HiveMindMeshIntegrationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<Hive?> CreateHiveAsync(HiveCreateRequest request) =>
            SendAsync<Hive?>(HttpMethod.Post, "hives", request);

        public Task<BatchCreateDronesResponse?> BatchCreateDronesAsync(BatchCreateDronesRequest request) =>
            SendAsync<BatchCreateDronesResponse>(HttpMethod.Post, "drones/batch", request);

        public Task<BatchJoinDronesResponse?> BatchJoinDronesAsync(string hiveId, BatchJoinDronesRequest request) =>
            SendAsync<BatchJoinDronesResponse>(HttpMethod.Post, $"hives/{hiveId}/drones/batch-join", request);

        public Task<TopologyRebuildResponse?> RebuildTopologyAsync(string hiveId, TopologyRebuildRequest request) =>
            SendAsync<TopologyRebuildResponse>(HttpMethod.Post, $"hives/{hiveId}/topology/rebuild", request);

        public Task<TopologyRebuildResponse?> ConnectHiveMindAsync(string hiveId, ConnectToHiveMindRequest request) =>
            SendAsync<TopologyRebuildResponse>(HttpMethod.Post, $"hives/{hiveId}/topology/connect-hivemind", request);

        public Task<SwarmConnectivityResponse?> GetConnectivityAsync(string hiveId) =>
            SendAsync<SwarmConnectivityResponse>(HttpMethod.Get, $"hives/{hiveId}/topology/connectivity");

        public Task<DegradeConnectionResponse?> DegradeConnectionAsync(DegradeConnectionRequest request) =>
            SendAsync<DegradeConnectionResponse>(HttpMethod.Post, "drones/connections/degrade", request);

        public Task<BatchDegradeConnectionsResponse?> BatchDegradeConnectionsAsync(BatchDegradeConnectionsRequest request) =>
            SendAsync<BatchDegradeConnectionsResponse>(HttpMethod.Post, "drones/connections/batch-degrade", request);

        public async Task LogConnectivitySnapshotAsync(string hiveId)
        {
            var snapshot = await GetConnectivityAsync(hiveId);
            if (snapshot == null || snapshot.TotalDrones <= 0)
            {
                return;
            }

            var isolatedSummary = snapshot.IsolatedGroups.Any()
                ? string.Join(" | ", snapshot.IsolatedGroups.Select(group => $"[{string.Join(",", group)}]"))
                : "none";

            _logger.LogInformation(
                "HiveMind connectivity snapshot for {HiveId}: components={Components}, largestComponentSize={LargestSize}, isolatedGroups={IsolatedGroups}",
                hiveId,
                snapshot.ConnectedComponents,
                snapshot.LargestComponentSize,
                isolatedSummary);

            if (snapshot.ConnectionGraph.Any())
            {
                foreach (var entry in snapshot.ConnectionGraph)
                {
                    var connections = entry.Value?.Any() == true
                        ? string.Join(", ", entry.Value.Select(c => $"{c.TargetDroneId}({c.Weight:F2})"))
                        : "none";

                    _logger.LogInformation("Hive {HiveId}: {DroneId} -> {Connections}",
                        hiveId, entry.Key, connections);
                }
            }
            else
            {
                _logger.LogInformation("Hive {HiveId}: no recorded connections in graph.", hiveId);
            }
        }

        private async Task<T?> SendAsync<T>(HttpMethod method, string relativePath, object? payload = null)
        {
            var request = new HttpRequestMessage(method, BuildUri(relativePath));
            if (payload != null && method != HttpMethod.Get)
            {
                var json = JsonSerializer.Serialize(payload, _serializerOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var client = _httpClientFactory.CreateClient(HiveMindClientName);
            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("HiveMind integration call {Method} {Path} failed with status {Status}. Response: {Response}",
                    method, relativePath, response.StatusCode, error);
                return default;
            }

            if (response.Content == null)
            {
                return default;
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(content, _serializerOptions);
        }

        private Uri BuildUri(string relativePath)
        {
            var config = _configuration.CurrentValue;
            var builder = new StringBuilder();
            builder.Append(config.HiveMindSchema ?? "http");
            builder.Append("://");
            builder.Append(config.HiveMindHost ?? "localhost");
            builder.Append(':');
            builder.Append(config.HiveMindPort);
            builder.Append('/');
            builder.Append((config.HiveMindPath ?? "api/v1").Trim('/'));
            builder.Append('/');
            builder.Append(relativePath.TrimStart('/'));

            return new Uri(builder.ToString(), UriKind.Absolute);
        }
    }
}

