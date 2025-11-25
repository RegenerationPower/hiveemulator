using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.hive;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class DroneRelayService : IDroneRelayService
    {
        private readonly ILogger<DroneRelayService> _logger;

        public DroneRelayService(ILogger<DroneRelayService> logger)
        {
            _logger = logger;
        }

        public IReadOnlyCollection<Drone> GetSwarm()
        {
            return HiveInMemoryState.Drones;
        }

        public bool UpsertDrone(Drone drone)
        {
            bool isNew = HiveInMemoryState.UpsertDrone(drone);
            if (isNew)
            {
                _logger.LogInformation("New drone {DroneId} of type {Type} registered with {ConnectionCount} connections.",
                    drone.Id, drone.Type, drone.Connections?.Count ?? 0);
            }
            else
            {
                _logger.LogInformation("Existing drone {DroneId} of type {Type} updated with {ConnectionCount} connections.",
                    drone.Id, drone.Type, drone.Connections?.Count ?? 0);
            }
            return isNew;
        }

        public bool RemoveDrone(string droneId)
        {
            // Remove from hive first
            HiveInMemoryState.RemoveDroneFromHive(droneId);
            
            // Remove all commands for this drone (completely remove from dictionary)
            HiveInMemoryState.RemoveDroneCommands(droneId);
            
            var removed = HiveInMemoryState.RemoveDrone(droneId);
            if (removed)
            {
                _logger.LogInformation("Drone {DroneId} removed from swarm, hive, and all commands deleted.", droneId);
            }
            else
            {
                _logger.LogWarning("Attempted to remove drone {DroneId}, but it was not found.", droneId);
            }

            return removed;
        }

        public DroneConnectionAnalysisResponse AnalyzeConnection(string droneId, double minimumWeight = 0.5)
        {
            var response = new DroneConnectionAnalysisResponse
            {
                TargetDroneId = droneId,
                Path = Array.Empty<string>(),
                MinimumLinkWeight = 0
            };

            var swarm = HiveInMemoryState.Drones;
            if (!swarm.Any())
            {
                _logger.LogWarning("Connection analysis requested for {DroneId}, but swarm is empty.", droneId);
                return response;
            }

            if (swarm.All(d => d.Id != droneId))
            {
                _logger.LogWarning("Connection analysis requested for unknown drone {DroneId}.", droneId);
                return response;
            }

            var relayEntryPoints = swarm.Where(d => d.Type == DroneType.Relay).Select(d => d.Id).ToList();
            if (!relayEntryPoints.Any())
            {
                _logger.LogWarning("Cannot evaluate connection to {DroneId} because no relay drones are registered.", droneId);
                return response;
            }

            // BFS to find shortest path with best quality (highest minimum weight)
            // BFS naturally finds the shortest path, and we track minimum weight along the path
            var parents = new Dictionary<string, string>();
            var minWeightTracker = new Dictionary<string, double>();
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            var adjacency = swarm.ToDictionary(d => d.Id, d => d.Connections ?? new List<DroneConnection>());

            // Initialize all relay entry points
            foreach (var relayId in relayEntryPoints)
            {
                queue.Enqueue(relayId);
                visited.Add(relayId);
                minWeightTracker[relayId] = double.PositiveInfinity;
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == droneId)
                {
                    // Found target - BFS guarantees this is the shortest path
                    var path = BuildPath(current, parents);
                    response.CanConnect = true;
                    response.Path = path;
                    response.EntryRelayDroneId = path.FirstOrDefault();
                    var minWeight = minWeightTracker[current];
                    response.MinimumLinkWeight = double.IsInfinity(minWeight) ? 1 : minWeight;
                    return response;
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

            _logger.LogWarning("HiveMind cannot reach drone {DroneId} with the current relay configuration.", droneId);
            return response;
        }

        public MeshCommandResponse SendCommandViaMesh(string targetDroneId, DroneCommand command, double minimumWeight = 0.5)
        {
            var response = new MeshCommandResponse
            {
                TargetDroneId = targetDroneId,
                RoutePath = Array.Empty<string>(),
                Success = false
            };

            // First, analyze connection to find the best route
            var analysis = AnalyzeConnection(targetDroneId, minimumWeight);
            
            if (!analysis.CanConnect || !analysis.Path.Any())
            {
                response.ErrorMessage = $"Cannot reach drone {targetDroneId} through mesh network. No valid route found.";
                _logger.LogWarning("Cannot send command to {DroneId} via mesh: {Error}", targetDroneId, response.ErrorMessage);
                return response;
            }

            var routePath = analysis.Path.ToList();
            response.RoutePath = routePath;
            response.MinimumLinkWeight = analysis.MinimumLinkWeight;
            response.HopCount = analysis.HopCount;

            // If the route has only one hop (direct connection from relay), send command directly
            if (routePath.Count <= 1)
            {
                // Direct connection - send command directly to target
                command.TargetDroneId = targetDroneId;
                command.CommandId = Guid.NewGuid();
                if (command.Timestamp == default)
                {
                    command.Timestamp = DateTime.UtcNow;
                }
                HiveInMemoryState.AddDroneCommand(command);
                response.Success = true;
                response.RelaysUsed = 0;
                _logger.LogInformation("Command sent directly to {DroneId} (no relay needed)", targetDroneId);
                return response;
            }

            // Send relay commands to intermediate drones
            int relaysUsed = 0;
            for (int i = 0; i < routePath.Count - 1; i++)
            {
                var currentDroneId = routePath[i];
                var nextDroneId = routePath[i + 1];
                var isFinalHop = (i == routePath.Count - 2);

                // Create relay command for intermediate drone
                var relayCommand = new DroneCommand
                {
                    CommandId = Guid.NewGuid(),
                    TargetDroneId = currentDroneId,
                    CommandType = DroneCommandType.Relay,
                    Timestamp = DateTime.UtcNow,
                    CommandPayload = new RelayDroneCommandPayload
                    {
                        FinalDestinationDroneId = targetDroneId,
                        NextHopDroneId = isFinalHop ? null : nextDroneId,
                        FinalCommand = command,
                        RoutePath = routePath
                    }
                };

                HiveInMemoryState.AddDroneCommand(relayCommand);
                relaysUsed++;
                _logger.LogInformation("Relay command sent to {CurrentDroneId} for routing to {TargetDroneId} via {NextDroneId}",
                    currentDroneId, targetDroneId, nextDroneId);
            }

            // Send final command to target drone
            command.TargetDroneId = targetDroneId;
            command.CommandId = Guid.NewGuid();
            if (command.Timestamp == default)
            {
                command.Timestamp = DateTime.UtcNow;
            }
            HiveInMemoryState.AddDroneCommand(command);

            response.Success = true;
            response.RelaysUsed = relaysUsed;
            _logger.LogInformation("Command sent to {TargetDroneId} via mesh network using {RelayCount} relay(s). Route: {Route}",
                targetDroneId, relaysUsed, string.Join(" -> ", routePath));

            return response;
        }

        private static IReadOnlyCollection<string> BuildPath(string targetDroneId, IReadOnlyDictionary<string, string> parents)
        {
            var path = new List<string>();
            var current = targetDroneId;
            path.Add(current);

            while (parents.TryGetValue(current, out var parent))
            {
                path.Add(parent);
                current = parent;
            }

            path.Reverse();
            return path;
        }
    }
}

