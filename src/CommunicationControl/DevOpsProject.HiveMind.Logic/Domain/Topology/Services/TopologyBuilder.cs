#nullable enable
using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using DevOpsProject.Shared.Enums;
using Models = DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Implementation of topology building logic
    /// </summary>
    public class TopologyBuilder : ITopologyBuilder
    {
        private readonly IDroneRepository _droneRepository;
        private readonly ILogger<TopologyBuilder> _logger;

        public TopologyBuilder(IDroneRepository droneRepository, ILogger<TopologyBuilder> logger)
        {
            _droneRepository = droneRepository;
            _logger = logger;
        }

        public int BuildMeshTopology(IReadOnlyCollection<Models.Drone> drones, double defaultWeight)
        {
            int connectionsCreated = 0;
            var droneList = drones.ToList();

            for (int i = 0; i < droneList.Count; i++)
            {
                for (int j = i + 1; j < droneList.Count; j++)
                {
                    var drone1 = droneList[i];
                    var drone2 = droneList[j];

                    AddBidirectionalConnection(drone1, drone2, defaultWeight);
                    connectionsCreated += 2;
                }
            }

            _logger.LogDebug("Created {Count} connections for mesh topology with {DroneCount} drones",
                connectionsCreated, drones.Count);
            return connectionsCreated;
        }

        public int BuildStarTopology(IReadOnlyCollection<Models.Drone> drones, string hubDroneId, double defaultWeight)
        {
            var hub = drones.FirstOrDefault(d => d.Id == hubDroneId);
            if (hub == null)
            {
                _logger.LogWarning("Hub drone {HubId} not found in drone collection", hubDroneId);
                return 0;
            }

            int connectionsCreated = 0;
            foreach (var drone in drones.Where(d => d.Id != hubDroneId))
            {
                AddBidirectionalConnection(hub, drone, defaultWeight);
                connectionsCreated += 2;
            }

            _logger.LogDebug("Created {Count} connections for star topology with hub {HubId}",
                connectionsCreated, hubDroneId);
            return connectionsCreated;
        }

        public int BuildDualStarTopology(IReadOnlyCollection<Models.Drone> drones, string hub1Id, string hub2Id, double defaultWeight)
        {
            var hub1 = drones.FirstOrDefault(d => d.Id == hub1Id);
            var hub2 = drones.FirstOrDefault(d => d.Id == hub2Id);

            if (hub1 == null || hub2 == null)
            {
                _logger.LogWarning("One or both hub drones not found: {Hub1}, {Hub2}", hub1Id, hub2Id);
                return 0;
            }

            int connectionsCreated = 0;

            // Connect hubs
            AddBidirectionalConnection(hub1, hub2, defaultWeight);
            connectionsCreated += 2;

            // Connect each drone to both hubs
            foreach (var drone in drones.Where(d => d.Id != hub1Id && d.Id != hub2Id))
            {
                AddBidirectionalConnection(hub1, drone, defaultWeight);
                AddBidirectionalConnection(hub2, drone, defaultWeight);
                connectionsCreated += 4;
            }

            _logger.LogDebug("Created {Count} connections for dual-star topology with hubs {Hub1}, {Hub2}",
                connectionsCreated, hub1Id, hub2Id);
            return connectionsCreated;
        }

        public int RemoveAllConnections(IReadOnlyCollection<Models.Drone> drones)
        {
            int removed = 0;
            foreach (var drone in drones)
            {
                var connectionsToRemove = drone.Connections?.ToList() ?? new List<Models.DroneConnection>();
                foreach (var connection in connectionsToRemove)
                {
                    RemoveBidirectionalConnection(drone.Id, connection.TargetDroneId);
                    removed += 2;
                }
            }

            _logger.LogDebug("Removed {Count} connections from {DroneCount} drones",
                removed, drones.Count);
            return removed;
        }

        private void AddBidirectionalConnection(Models.Drone drone1, Models.Drone drone2, double weight)
        {
            var connection1 = new Models.DroneConnection
            {
                TargetDroneId = drone2.Id,
                Weight = GetLinkWeight(weight)
            };

            var connection2 = new Models.DroneConnection
            {
                TargetDroneId = drone1.Id,
                Weight = GetLinkWeight(weight)
            };

            if (drone1.Connections == null) drone1.Connections = new List<Models.DroneConnection>();
            if (drone2.Connections == null) drone2.Connections = new List<Models.DroneConnection>();

            RemoveConnectionIfExists(drone1.Connections, drone2.Id);
            RemoveConnectionIfExists(drone2.Connections, drone1.Id);

            drone1.Connections.Add(connection1);
            drone2.Connections.Add(connection2);

            _droneRepository.Update(drone1);
            _droneRepository.Update(drone2);
        }

        private void RemoveBidirectionalConnection(string drone1Id, string drone2Id)
        {
            var drone1 = _droneRepository.GetById(drone1Id);
            var drone2 = _droneRepository.GetById(drone2Id);

            if (drone1?.Connections != null)
            {
                RemoveConnectionIfExists(drone1.Connections, drone2Id);
                _droneRepository.Update(drone1);
            }

            if (drone2?.Connections != null)
            {
                RemoveConnectionIfExists(drone2.Connections, drone1Id);
                _droneRepository.Update(drone2);
            }
        }

        private static void RemoveConnectionIfExists(List<Models.DroneConnection>? connections, string targetDroneId)
        {
            if (connections == null) return;
            var existing = connections.FirstOrDefault(c => c.TargetDroneId == targetDroneId);
            if (existing != null)
            {
                connections.Remove(existing);
            }
        }

        private static double GetLinkWeight(double baseWeight)
        {
            var random = new Random();
            var jitter = (random.NextDouble() - 0.5) * 0.1;
            return Math.Max(0.0, Math.Min(1.0, baseWeight + jitter));
        }
    }
}

