#nullable enable
using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Enums;
using Models = DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.DTO.Topology;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Analyzes network connectivity and routing paths
    /// </summary>
    public class ConnectivityAnalyzer : IConnectivityAnalyzer
    {
        private readonly IDroneRepository _droneRepository;
        private readonly IHiveRepository _hiveRepository;
        private readonly ILogger<ConnectivityAnalyzer> _logger;

        public ConnectivityAnalyzer(
            IDroneRepository droneRepository,
            IHiveRepository hiveRepository,
            ILogger<ConnectivityAnalyzer> logger)
        {
            _droneRepository = droneRepository;
            _hiveRepository = hiveRepository;
            _logger = logger;
        }

        public DroneConnectionAnalysisResponse AnalyzeConnection(string targetDroneId, double minimumWeight = 0.5)
        {
            var response = new DroneConnectionAnalysisResponse
            {
                TargetDroneId = targetDroneId,
                Path = Array.Empty<string>(),
                MinimumLinkWeight = 0
            };

            var swarm = _droneRepository.GetAll();
            if (!swarm.Any())
            {
                _logger.LogWarning("Connection analysis requested for {DroneId}, but swarm is empty.", targetDroneId);
                return response;
            }

            if (!_droneRepository.Exists(targetDroneId))
            {
                _logger.LogWarning("Connection analysis requested for unknown drone {DroneId}.", targetDroneId);
                return response;
            }

            var entryRelays = GetEntryRelaysForDrone(targetDroneId, swarm);
            if (!entryRelays.Any())
            {
                _logger.LogWarning("Cannot evaluate connection to {DroneId} because no relay drones are registered.", targetDroneId);
                return response;
            }

            var pathResult = FindShortestPath(targetDroneId, swarm, entryRelays, minimumWeight);
            if (pathResult != null)
            {
                response.CanConnect = true;
                response.Path = pathResult.Path;
                response.PathWeights = pathResult.Weights;
                response.EntryRelayDroneId = pathResult.Path.FirstOrDefault();
                response.MinimumLinkWeight = pathResult.MinWeight;

                LogPath(targetDroneId, pathResult.Path, pathResult.Weights);
            }

            return response;
        }

        public SwarmConnectivityResponse AnalyzeSwarmConnectivity(string hiveId)
        {
            var response = new SwarmConnectivityResponse
            {
                HiveId = hiveId
            };

            if (!_hiveRepository.Exists(hiveId))
            {
                _logger.LogWarning("Connectivity analysis requested for non-existent hive {HiveId}", hiveId);
                return response;
            }

            var droneIds = _hiveRepository.GetDroneIds(hiveId);
            if (!droneIds.Any())
            {
                _logger.LogWarning("Hive {HiveId} has no drones", hiveId);
                return response;
            }

            var drones = _droneRepository.GetAll()
                .Where(d => droneIds.Contains(d.Id))
                .ToList();

            response.TotalDrones = drones.Count;

            if (drones.Count == 0)
            {
                return response;
            }

            var components = FindConnectedComponents(drones);
            response.ConnectedComponents = components.Count;
            response.IsFullyConnected = components.Count == 1;
            response.LargestComponentSize = components.Max(c => c.Count);
            response.Components = components.Select((c, idx) => new ComponentInfo
            {
                ComponentId = idx + 1,
                DroneCount = c.Count,
                DroneIds = c,
                ConnectionCount = CountConnectionsInComponent(c, drones)
            }).ToList();

            var allConnections = GetAllConnections(drones);
            response.TotalConnections = allConnections.Count;
            if (allConnections.Any())
            {
                response.AverageConnectionWeight = allConnections.Average(c => c.Weight);
                response.MinimumConnectionWeight = allConnections.Min(c => c.Weight);
                response.MaximumConnectionWeight = allConnections.Max(c => c.Weight);
            }

            response.ConnectionGraph = BuildConnectionGraph(drones);
            response.IsolatedGroups = components.Where(c => c.Count < response.LargestComponentSize).ToList();

            return response;
        }

        private List<string> GetEntryRelaysForDrone(string droneId, IReadOnlyCollection<Models.Drone> swarm)
        {
            var hiveId = HiveInMemoryState.GetDroneHive(droneId);
            if (!string.IsNullOrWhiteSpace(hiveId))
            {
                var configuredRelays = _hiveRepository.GetEntryRelays(hiveId);
                if (configuredRelays.Any())
                {
                    return configuredRelays.ToList();
                }
            }

            return swarm
                .Where(d => d.Type == DroneType.Relay)
                .Select(d => d.Id)
                .ToList();
        }

        private PathResult? FindShortestPath(
            string targetDroneId,
            IReadOnlyCollection<Models.Drone> swarm,
            List<string> entryRelays,
            double minimumWeight)
        {
            var parents = new Dictionary<string, string>();
            var minWeightTracker = new Dictionary<string, double>();
            var visited = new HashSet<string>();
            var queue = new Queue<string>();

            var adjacency = swarm.ToDictionary(d => d.Id, d => d.Connections ?? new List<Models.DroneConnection>());
            var weightLookup = adjacency.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(conn => conn.TargetDroneId, conn => conn.Weight));

            foreach (var relayId in entryRelays)
            {
                queue.Enqueue(relayId);
                visited.Add(relayId);
                minWeightTracker[relayId] = double.PositiveInfinity;
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == targetDroneId)
                {
                    var (pathNodes, pathWeights) = BuildPathWithWeights(current, parents, weightLookup);
                    var minWeight = minWeightTracker[current];
                    return new PathResult
                    {
                        Path = pathNodes,
                        Weights = pathWeights,
                        MinWeight = double.IsInfinity(minWeight) ? 1 : minWeight
                    };
                }

                if (!adjacency.TryGetValue(current, out var connections) || connections.Count == 0)
                {
                    continue;
                }

                foreach (var connection in connections.Where(conn => conn.Weight >= minimumWeight))
                {
                    if (!visited.Add(connection.TargetDroneId))
                    {
                        continue;
                    }

                    parents[connection.TargetDroneId] = current;
                    var previousWeight = minWeightTracker.TryGetValue(current, out var currentWeight)
                        ? currentWeight
                        : double.PositiveInfinity;
                    minWeightTracker[connection.TargetDroneId] = Math.Min(previousWeight, connection.Weight);
                    queue.Enqueue(connection.TargetDroneId);
                }
            }

            return null;
        }

        private static (List<string> pathNodes, List<double> pathWeights) BuildPathWithWeights(
            string target,
            Dictionary<string, string> parents,
            Dictionary<string, Dictionary<string, double>> weightLookup)
        {
            var pathNodes = new List<string>();
            var pathWeights = new List<double>();
            var current = target;

            while (parents.TryGetValue(current, out var parent))
            {
                pathNodes.Insert(0, current);
                if (weightLookup.TryGetValue(parent, out var connections) &&
                    connections.TryGetValue(current, out var weight))
                {
                    pathWeights.Insert(0, weight);
                }
                current = parent;
            }

            pathNodes.Insert(0, current);
            return (pathNodes, pathWeights);
        }

        private List<List<string>> FindConnectedComponents(List<Models.Drone> drones)
        {
            var visited = new HashSet<string>();
            var components = new List<List<string>>();

            foreach (var drone in drones)
            {
                if (visited.Contains(drone.Id))
                {
                    continue;
                }

                var component = new List<string>();
                var queue = new Queue<string>();
                queue.Enqueue(drone.Id);
                visited.Add(drone.Id);

                while (queue.Count > 0)
                {
                    var currentId = queue.Dequeue();
                    component.Add(currentId);

                    var currentDrone = drones.FirstOrDefault(d => d.Id == currentId);
                    if (currentDrone?.Connections == null)
                    {
                        continue;
                    }

                    foreach (var connection in currentDrone.Connections)
                    {
                        if (visited.Add(connection.TargetDroneId))
                        {
                            var targetDrone = drones.FirstOrDefault(d => d.Id == connection.TargetDroneId);
                            if (targetDrone != null)
                            {
                                queue.Enqueue(connection.TargetDroneId);
                            }
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }

        private static int CountConnectionsInComponent(List<string> component, List<Models.Drone> drones)
        {
            int count = 0;
            foreach (var droneId in component)
            {
                var drone = drones.FirstOrDefault(d => d.Id == droneId);
                if (drone?.Connections != null)
                {
                    count += drone.Connections.Count(conn => component.Contains(conn.TargetDroneId));
                }
            }
            return count;
        }

        private static List<Models.DroneConnection> GetAllConnections(List<Models.Drone> drones)
        {
            var connections = new List<Models.DroneConnection>();
            foreach (var drone in drones)
            {
                if (drone.Connections != null)
                {
                    connections.AddRange(drone.Connections);
                }
            }
            return connections;
        }

        private static Dictionary<string, IReadOnlyCollection<ConnectionInfo>> BuildConnectionGraph(List<Models.Drone> drones)
        {
            var graph = new Dictionary<string, IReadOnlyCollection<ConnectionInfo>>();

            foreach (var drone in drones)
            {
                var connections = drone.Connections?
                    .Select(c => new ConnectionInfo
                    {
                        TargetDroneId = c.TargetDroneId,
                        Weight = c.Weight
                    })
                    .ToList() ?? new List<ConnectionInfo>();

                graph[drone.Id] = connections;
            }

            return graph;
        }

        private void LogPath(string targetDroneId, List<string> pathNodes, List<double> pathWeights)
        {
            if (pathNodes.Count > 1)
            {
                var segments = new List<string>();
                for (int i = 0; i < pathNodes.Count - 1; i++)
                {
                    var segmentWeight = i < pathWeights.Count ? pathWeights[i] : 0;
                    segments.Add($"{pathNodes[i]}->{pathNodes[i + 1]}({segmentWeight:F2})");
                }
                _logger.LogInformation("Analyze route to {Target}: {Segments}",
                    targetDroneId, string.Join(" | ", segments));
            }
        }

        private class PathResult
        {
            public List<string> Path { get; set; } = new();
            public List<double> Weights { get; set; } = new();
            public double MinWeight { get; set; }
        }
    }
}

