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

        public BatchCreateDronesResponse BatchCreateDrones(BatchCreateDronesRequest request)
        {
            var response = new BatchCreateDronesResponse
            {
                TotalRequested = request?.Drones?.Count ?? 0
            };

            if (request == null || request.Drones == null || !request.Drones.Any())
            {
                response.Errors = new[] { "Request cannot be null and must contain at least one drone" };
                return response;
            }

            var createdIds = new List<string>();
            var updatedIds = new List<string>();
            var errors = new List<string>();

            foreach (var drone in request.Drones)
            {
                if (drone == null)
                {
                    errors.Add("One or more drones in the batch are null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(drone.Id))
                {
                    errors.Add("One or more drones have empty or null ID");
                    continue;
                }

                try
                {
                    bool isNew = UpsertDrone(drone);
                    if (isNew)
                    {
                        createdIds.Add(drone.Id);
                    }
                    else
                    {
                        updatedIds.Add(drone.Id);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to create/update drone {drone.Id}: {ex.Message}");
                }
            }

            response.Created = createdIds.Count;
            response.Updated = updatedIds.Count;
            response.Failed = errors.Count;
            response.CreatedDroneIds = createdIds;
            response.UpdatedDroneIds = updatedIds;
            response.Errors = errors;

            _logger.LogInformation("Batch create completed: {Created} created, {Updated} updated, {Failed} failed out of {Total}",
                response.Created, response.Updated, response.Failed, response.TotalRequested);

            return response;
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

        public TopologyRebuildResponse RebuildTopology(TopologyRebuildRequest request)
        {
            var response = new TopologyRebuildResponse
            {
                HiveId = request.HiveId,
                TopologyType = request.TopologyType,
                Success = false
            };

            // Check if Hive exists
            var hive = HiveInMemoryState.GetHive(request.HiveId);
            if (hive == null)
            {
                response.ErrorMessage = $"Hive {request.HiveId} not found.";
                return response;
            }

            // Get all drones in the Hive
            var hiveDrones = HiveInMemoryState.GetHiveDrones(request.HiveId).ToList();
            if (hiveDrones.Count < 2)
            {
                response.ErrorMessage = $"Hive {request.HiveId} has less than 2 drones. Cannot build topology.";
                return response;
            }

            var swarm = HiveInMemoryState.Drones;
            var dronesInHive = swarm.Where(d => hiveDrones.Contains(d.Id)).ToList();

            int connectionsCreated = 0;
            int connectionsRemoved = 0;

            switch (request.TopologyType.ToLower())
            {
                case "mesh":
                    // Full mesh: every drone connects to every other drone
                    connectionsRemoved = RemoveAllConnections(dronesInHive);
                    connectionsCreated = CreateFullMeshTopology(dronesInHive, request.DefaultWeight);
                    break;

                case "star":
                    // Star topology: one central hub (prefer Relay drone)
                    var hub = dronesInHive.FirstOrDefault(d => d.Type == DroneType.Relay) 
                             ?? dronesInHive.First();
                    connectionsRemoved = RemoveAllConnections(dronesInHive);
                    connectionsCreated = CreateStarTopology(dronesInHive, hub.Id, request.DefaultWeight);
                    break;

                case "dual_star":
                    // Dual-star: two hubs, each drone connects to both
                    var relays = dronesInHive.Where(d => d.Type == DroneType.Relay).Take(2).ToList();
                    if (relays.Count < 2 && dronesInHive.Count >= 2)
                    {
                        // Use first two drones if not enough relays
                        relays = dronesInHive.Take(2).ToList();
                    }
                    if (relays.Count < 2)
                    {
                        response.ErrorMessage = "Dual-star topology requires at least 2 drones.";
                        return response;
                    }
                    connectionsRemoved = RemoveAllConnections(dronesInHive);
                    connectionsCreated = CreateDualStarTopology(dronesInHive, relays[0].Id, relays[1].Id, request.DefaultWeight);
                    break;

                default:
                    response.ErrorMessage = $"Unknown topology type: {request.TopologyType}. Supported: mesh, star, dual_star";
                    return response;
            }

            response.Success = true;
            response.ConnectionsCreated = connectionsCreated;
            response.ConnectionsRemoved = connectionsRemoved;
            _logger.LogInformation("Topology rebuilt for Hive {HiveId}: {TopologyType}, {Created} created, {Removed} removed",
                request.HiveId, request.TopologyType, connectionsCreated, connectionsRemoved);

            return response;
        }

        public TopologyRebuildResponse ConnectToHiveMind(ConnectToHiveMindRequest request)
        {
            var response = new TopologyRebuildResponse
            {
                HiveId = request.HiveId,
                TopologyType = request.TopologyType,
                Success = false
            };

            // Check if Hive exists
            var hive = HiveInMemoryState.GetHive(request.HiveId);
            if (hive == null)
            {
                response.ErrorMessage = $"Hive {request.HiveId} not found.";
                return response;
            }

            // Get all drones in the Hive
            var hiveDrones = HiveInMemoryState.GetHiveDrones(request.HiveId).ToList();
            if (!hiveDrones.Any())
            {
                response.ErrorMessage = $"Hive {request.HiveId} has no drones.";
                return response;
            }

            var swarm = HiveInMemoryState.Drones;
            var dronesInHive = swarm.Where(d => hiveDrones.Contains(d.Id)).ToList();

            // Find relay drones (entry points to HiveMind)
            var relayDrones = swarm.Where(d => d.Type == DroneType.Relay).ToList();
            if (!relayDrones.Any())
            {
                response.ErrorMessage = "No relay drones found. Cannot connect to HiveMind.";
                return response;
            }

            int connectionsCreated = 0;

            switch (request.TopologyType.ToLower())
            {
                case "star":
                    // Connect all drones to one relay drone
                    var hub = request.HubDroneIds?.FirstOrDefault() != null
                        ? relayDrones.FirstOrDefault(d => d.Id == request.HubDroneIds[0])
                        : relayDrones.First();
                    
                    if (hub == null)
                    {
                        response.ErrorMessage = "Specified hub drone not found or is not a relay.";
                        return response;
                    }

                    connectionsCreated = ConnectDronesToHub(dronesInHive, hub.Id, request.ConnectionWeight);
                    break;

                case "dual_star":
                    // Connect all drones to two relay drones
                    var hubs = new List<Drone>();
                    if (request.HubDroneIds != null && request.HubDroneIds.Count >= 2)
                    {
                        hubs = relayDrones.Where(d => request.HubDroneIds.Contains(d.Id)).Take(2).ToList();
                    }
                    else
                    {
                        hubs = relayDrones.Take(2).ToList();
                    }

                    if (hubs.Count < 2)
                    {
                        response.ErrorMessage = "Dual-star topology requires at least 2 relay drones.";
                        return response;
                    }

                    connectionsCreated = ConnectDronesToDualHub(dronesInHive, hubs[0].Id, hubs[1].Id, request.ConnectionWeight);
                    break;

                default:
                    response.ErrorMessage = $"Unknown topology type: {request.TopologyType}. Supported: star, dual_star";
                    return response;
            }

            response.Success = true;
            response.ConnectionsCreated = connectionsCreated;
            _logger.LogInformation("Connected {Count} drones to HiveMind using {TopologyType} topology",
                dronesInHive.Count, request.TopologyType);

            return response;
        }

        public SwarmConnectivityResponse AnalyzeSwarmConnectivity(string hiveId)
        {
            var response = new SwarmConnectivityResponse
            {
                HiveId = hiveId,
                IsFullyConnected = false,
                ConnectedComponents = 0
            };

            // Check if Hive exists
            var hive = HiveInMemoryState.GetHive(hiveId);
            if (hive == null)
            {
                return response;
            }

            // Get all drones in the Hive
            var hiveDrones = HiveInMemoryState.GetHiveDrones(hiveId).ToList();
            if (!hiveDrones.Any())
            {
                response.TotalDrones = 0;
                return response;
            }

            response.TotalDrones = hiveDrones.Count;

            var swarm = HiveInMemoryState.Drones;
            var dronesInHive = swarm.Where(d => hiveDrones.Contains(d.Id)).ToList();

            // Build adjacency list and connection graph with weights
            var adjacency = new Dictionary<string, List<string>>();
            var connectionGraph = new Dictionary<string, List<ConnectionInfo>>();
            var allWeights = new List<double>();
            int totalConnections = 0;

            foreach (var drone in dronesInHive)
            {
                adjacency[drone.Id] = new List<string>();
                connectionGraph[drone.Id] = new List<ConnectionInfo>();

                foreach (var connection in drone.Connections)
                {
                    if (hiveDrones.Contains(connection.TargetDroneId))
                    {
                        adjacency[drone.Id].Add(connection.TargetDroneId);
                        connectionGraph[drone.Id].Add(new ConnectionInfo
                        {
                            TargetDroneId = connection.TargetDroneId,
                            Weight = connection.Weight
                        });
                        allWeights.Add(connection.Weight);
                        totalConnections++;
                    }
                }
            }

            // Find connected components using BFS
            var visited = new HashSet<string>();
            var components = new List<List<string>>();

            foreach (var drone in dronesInHive)
            {
                if (visited.Contains(drone.Id))
                    continue;

                var component = new List<string>();
                var queue = new Queue<string>();
                queue.Enqueue(drone.Id);
                visited.Add(drone.Id);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(current);

                    if (adjacency.TryGetValue(current, out var neighbors))
                    {
                        foreach (var neighbor in neighbors)
                        {
                            if (visited.Add(neighbor))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }

                components.Add(component);
            }

            // Build component info
            var componentInfos = new List<ComponentInfo>();
            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                int componentConnections = 0;
                
                // Count connections within this component
                foreach (var droneId in component)
                {
                    if (adjacency.TryGetValue(droneId, out var neighbors))
                    {
                        componentConnections += neighbors.Count(d => component.Contains(d));
                    }
                }

                componentInfos.Add(new ComponentInfo
                {
                    ComponentId = i + 1,
                    DroneCount = component.Count,
                    DroneIds = component,
                    ConnectionCount = componentConnections / 2 // Divide by 2 because each connection is counted twice (bidirectional)
                });
            }

            response.ConnectedComponents = components.Count;
            response.IsFullyConnected = components.Count == 1;
            response.LargestComponentSize = components.Any() ? components.Max(c => c.Count) : 0;
            response.IsolatedGroups = components.Where(c => c.Count < response.TotalDrones).Select(c => (IReadOnlyCollection<string>)c.ToList()).ToList();
            response.Components = componentInfos;
            response.TotalConnections = totalConnections / 2; // Divide by 2 because each connection is bidirectional
            response.ConnectionGraph = connectionGraph.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyCollection<ConnectionInfo>)kvp.Value
            );

            if (allWeights.Any())
            {
                response.AverageConnectionWeight = allWeights.Average();
                response.MinimumConnectionWeight = allWeights.Min();
                response.MaximumConnectionWeight = allWeights.Max();
            }

            return response;
        }

        public DegradeConnectionResponse DegradeConnection(DegradeConnectionRequest request)
        {
            var response = new DegradeConnectionResponse
            {
                FromDroneId = request.FromDroneId,
                ToDroneId = request.ToDroneId,
                NewWeight = request.NewWeight,
                Success = false
            };

            if (string.IsNullOrWhiteSpace(request.FromDroneId) || string.IsNullOrWhiteSpace(request.ToDroneId))
            {
                response.ErrorMessage = "FromDroneId and ToDroneId are required.";
                return response;
            }

            if (request.NewWeight < 0.0 || request.NewWeight > 1.0)
            {
                response.ErrorMessage = "NewWeight must be between 0.0 and 1.0.";
                return response;
            }

            // Get the source drone
            var fromDrone = HiveInMemoryState.GetDrone(request.FromDroneId);
            if (fromDrone == null)
            {
                response.ErrorMessage = $"Drone {request.FromDroneId} not found.";
                return response;
            }

            // Check if connection exists
            var connection = fromDrone.Connections.FirstOrDefault(c => c.TargetDroneId == request.ToDroneId);
            if (connection == null)
            {
                response.ErrorMessage = $"Connection from {request.FromDroneId} to {request.ToDroneId} does not exist.";
                return response;
            }

            // Store previous weight
            response.PreviousWeight = connection.Weight;

            if (request.NewWeight <= 0)
            {
                RemoveConnection(request.FromDroneId, request.ToDroneId);
                RemoveConnection(request.ToDroneId, request.FromDroneId);
                response.NewWeight = 0;
                response.Success = true;
                _logger.LogInformation("Connection removed due to zero weight: {FromDroneId} <-> {ToDroneId}", request.FromDroneId, request.ToDroneId);
                return response;
            }

            // Update connection weight
            connection.Weight = request.NewWeight;
            HiveInMemoryState.UpsertDrone(fromDrone);

            // Also update reverse connection if it exists (bidirectional)
            var toDrone = HiveInMemoryState.GetDrone(request.ToDroneId);
            if (toDrone != null)
            {
                var reverseConnection = toDrone.Connections.FirstOrDefault(c => c.TargetDroneId == request.FromDroneId);
                if (reverseConnection != null)
                {
                    reverseConnection.Weight = request.NewWeight;
                    HiveInMemoryState.UpsertDrone(toDrone);
                }
            }

            response.Success = true;
            _logger.LogInformation("Connection degraded: {FromDroneId} -> {ToDroneId}, weight changed from {OldWeight} to {NewWeight}",
                request.FromDroneId, request.ToDroneId, response.PreviousWeight, request.NewWeight);

            return response;
        }

        public BatchDegradeConnectionsResponse BatchDegradeConnections(BatchDegradeConnectionsRequest request)
        {
            var response = new BatchDegradeConnectionsResponse
            {
                TotalRequested = request?.Connections?.Count ?? 0
            };

            if (request == null || request.Connections == null || !request.Connections.Any())
            {
                return response;
            }

            var successful = new List<DegradeConnectionResponse>();
            var failed = new List<DegradeConnectionResponse>();

            foreach (var connectionRequest in request.Connections)
            {
                var result = DegradeConnection(connectionRequest);
                if (result.Success)
                {
                    successful.Add(result);
                }
                else
                {
                    failed.Add(result);
                }
            }

            response.Succeeded = successful.Count;
            response.Failed = failed.Count;
            response.Successful = successful;
            response.FailedResults = failed;

            _logger.LogInformation("Batch degradation completed: {Succeeded} succeeded, {Failed} failed out of {Total}",
                response.Succeeded, response.Failed, response.TotalRequested);

            return response;
        }

        #region Private Helper Methods

        private int RemoveAllConnections(List<Drone> drones)
        {
            int removed = 0;
            var hiveDroneIds = drones.Select(d => d.Id).ToHashSet();
            
            foreach (var drone in drones)
            {
                var originalDrone = HiveInMemoryState.GetDrone(drone.Id);
                if (originalDrone != null && originalDrone.Connections.Any())
                {
                    // Remove only connections to other drones in the Hive
                    var connectionsToRemove = originalDrone.Connections
                        .Where(c => hiveDroneIds.Contains(c.TargetDroneId))
                        .ToList();
                    
                    foreach (var connection in connectionsToRemove)
                    {
                        originalDrone.Connections.Remove(connection);
                        removed++;
                    }
                    
                    HiveInMemoryState.UpsertDrone(originalDrone);
                }
            }
            return removed;
        }

        private int CreateFullMeshTopology(List<Drone> drones, double weight)
        {
            int created = 0;
            for (int i = 0; i < drones.Count; i++)
            {
                for (int j = i + 1; j < drones.Count; j++)
                {
                    AddConnection(drones[i].Id, drones[j].Id, weight);
                    AddConnection(drones[j].Id, drones[i].Id, weight);
                    created += 2;
                }
            }
            return created;
        }

        private int CreateStarTopology(List<Drone> drones, string hubId, double weight)
        {
            int created = 0;
            foreach (var drone in drones)
            {
                if (drone.Id != hubId)
                {
                    AddConnection(drone.Id, hubId, weight);
                    AddConnection(hubId, drone.Id, weight);
                    created += 2;
                }
            }
            return created;
        }

        private int CreateDualStarTopology(List<Drone> drones, string hub1Id, string hub2Id, double weight)
        {
            int created = 0;
            // Connect hubs to each other
            AddConnection(hub1Id, hub2Id, weight);
            AddConnection(hub2Id, hub1Id, weight);
            created += 2;

            // Connect all other drones to both hubs
            foreach (var drone in drones)
            {
                if (drone.Id != hub1Id && drone.Id != hub2Id)
                {
                    AddConnection(drone.Id, hub1Id, weight);
                    AddConnection(hub1Id, drone.Id, weight);
                    AddConnection(drone.Id, hub2Id, weight);
                    AddConnection(hub2Id, drone.Id, weight);
                    created += 4;
                }
            }
            return created;
        }

        private int ConnectDronesToHub(List<Drone> drones, string hubId, double weight)
        {
            int created = 0;
            foreach (var drone in drones)
            {
                if (drone.Id != hubId)
                {
                    AddConnection(drone.Id, hubId, weight);
                    AddConnection(hubId, drone.Id, weight);
                    created += 2;
                }
            }
            return created;
        }

        private int ConnectDronesToDualHub(List<Drone> drones, string hub1Id, string hub2Id, double weight)
        {
            int created = 0;
            // Connect hubs to each other
            AddConnection(hub1Id, hub2Id, weight);
            AddConnection(hub2Id, hub1Id, weight);
            created += 2;

            // Connect all drones to both hubs
            foreach (var drone in drones)
            {
                if (drone.Id != hub1Id && drone.Id != hub2Id)
                {
                    AddConnection(drone.Id, hub1Id, weight);
                    AddConnection(hub1Id, drone.Id, weight);
                    AddConnection(drone.Id, hub2Id, weight);
                    AddConnection(hub2Id, drone.Id, weight);
                    created += 4;
                }
            }
            return created;
        }

        private void AddConnection(string fromDroneId, string toDroneId, double weight)
        {
            if (weight <= 0)
            {
                RemoveConnection(fromDroneId, toDroneId);
                return;
            }

            var drone = HiveInMemoryState.GetDrone(fromDroneId);
            if (drone == null)
                return;

            // Check if connection already exists
            var existingConnection = drone.Connections.FirstOrDefault(c => c.TargetDroneId == toDroneId);
            if (existingConnection != null)
            {
                existingConnection.Weight = weight;
            }
            else
            {
                drone.Connections.Add(new DroneConnection
                {
                    TargetDroneId = toDroneId,
                    Weight = weight
                });
            }

            HiveInMemoryState.UpsertDrone(drone);
        }

        private void RemoveConnection(string fromDroneId, string toDroneId)
        {
            var drone = HiveInMemoryState.GetDrone(fromDroneId);
            if (drone == null)
            {
                return;
            }

            int removed = drone.Connections.RemoveAll(c => c.TargetDroneId == toDroneId);
            if (removed > 0)
            {
                HiveInMemoryState.UpsertDrone(drone);
                _logger.LogInformation("Connection removed: {FromDroneId} -> {ToDroneId}", fromDroneId, toDroneId);
            }
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

        #endregion
    }
}

