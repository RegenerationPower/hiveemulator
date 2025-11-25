using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
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

        public bool RemoveDrone(Guid droneId)
        {
            var removed = HiveInMemoryState.RemoveDrone(droneId);
            if (removed)
            {
                _logger.LogInformation("Drone {DroneId} removed from swarm.", droneId);
            }
            else
            {
                _logger.LogWarning("Attempted to remove drone {DroneId}, but it was not found.", droneId);
            }

            return removed;
        }

        public DroneConnectionAnalysisResponse AnalyzeConnection(Guid droneId, double minimumWeight = 0.5)
        {
            var response = new DroneConnectionAnalysisResponse
            {
                TargetDroneId = droneId,
                Path = Array.Empty<Guid>(),
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

            var parents = new Dictionary<Guid, Guid>();
            var minWeightTracker = new Dictionary<Guid, double>();
            var visited = new HashSet<Guid>();
            var queue = new Queue<Guid>();
            var adjacency = swarm.ToDictionary(d => d.Id, d => d.Connections ?? new List<DroneConnection>());

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

        private static IReadOnlyCollection<Guid> BuildPath(Guid targetDroneId, IReadOnlyDictionary<Guid, Guid> parents)
        {
            var path = new List<Guid>();
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

