using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Topology.Services;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.Drone;
using DevOpsProject.Shared.Models.DTO.Topology;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class DroneRelayService : IDroneRelayService
    {
        private readonly ILogger<DroneRelayService> _logger;
        private readonly IDroneRepository _droneRepository;
        private readonly IHiveRepository _hiveRepository;
        private readonly ITopologyBuilder _topologyBuilder;
        private readonly IConnectivityAnalyzer _connectivityAnalyzer;
        private readonly MeshRoutingService _meshRoutingService;
        private readonly IConnectionManager _connectionManager;

        public DroneRelayService(
            ILogger<DroneRelayService> logger,
            IDroneRepository droneRepository,
            IHiveRepository hiveRepository,
            ITopologyBuilder topologyBuilder,
            IConnectivityAnalyzer connectivityAnalyzer,
            MeshRoutingService meshRoutingService,
            IConnectionManager connectionManager)
        {
            _logger = logger;
            _droneRepository = droneRepository;
            _hiveRepository = hiveRepository;
            _topologyBuilder = topologyBuilder;
            _connectivityAnalyzer = connectivityAnalyzer;
            _meshRoutingService = meshRoutingService;
            _connectionManager = connectionManager;
        }

        public IReadOnlyCollection<Drone> GetSwarm()
        {
            return _droneRepository.GetAll();
        }

        public bool UpsertDrone(Drone drone)
        {
            bool isNew = _droneRepository.Add(drone);
            if (!isNew)
            {
                _droneRepository.Update(drone);
                _logger.LogInformation("Existing drone {DroneId} of type {Type} updated.",
                    drone.Id, drone.Type);
            }
            else
            {
                _logger.LogInformation("New drone {DroneId} of type {Type} registered.",
                    drone.Id, drone.Type);
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
            // Remove from all hives first
            var allHives = _hiveRepository.GetAll();
            foreach (var hive in allHives)
            {
                _hiveRepository.RemoveDroneFromHive(hive.Id, droneId);
            }
            
            // Remove all commands for this drone
            HiveInMemoryState.RemoveDroneCommands(droneId);
            
            var removed = _droneRepository.Remove(droneId);
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

        public int RemoveAllDrones()
        {
            var allDrones = _droneRepository.GetAll();
            int removedCount = 0;

            foreach (var drone in allDrones)
            {
                if (RemoveDrone(drone.Id))
                {
                    removedCount++;
                }
            }

            _logger.LogInformation("Removed all {Count} drones from the swarm", removedCount);
            return removedCount;
        }

        public DroneConnectionAnalysisResponse AnalyzeConnection(string droneId, double minimumWeight = 0.5)
        {
            return _connectivityAnalyzer.AnalyzeConnection(droneId, minimumWeight);
        }

        public MeshCommandResponse SendCommandViaMesh(string targetDroneId, DroneCommand command, double minimumWeight = 0.5)
        {
            return _meshRoutingService.RouteCommand(targetDroneId, command, minimumWeight);
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
            var hive = _hiveRepository.GetById(request.HiveId);
            if (hive == null)
            {
                response.ErrorMessage = $"Hive {request.HiveId} not found.";
                return response;
            }

            // Get all drones in the Hive
            var dronesInHive = _droneRepository.GetByHiveId(request.HiveId).ToList();
            if (dronesInHive.Count < 2)
            {
                response.ErrorMessage = $"Hive {request.HiveId} has less than 2 drones. Cannot build topology.";
                return response;
            }

            int connectionsCreated = 0;
            int connectionsRemoved = 0;

            connectionsRemoved = _topologyBuilder.RemoveAllConnections(dronesInHive);

            switch (request.TopologyType.ToLower())
            {
                case "mesh":
                    connectionsCreated = _topologyBuilder.BuildMeshTopology(dronesInHive, request.DefaultWeight);
                    break;

                case "star":
                    var hub = dronesInHive.FirstOrDefault(d => d.Type == DroneType.Relay) 
                             ?? dronesInHive.First();
                    connectionsCreated = _topologyBuilder.BuildStarTopology(dronesInHive, hub.Id, request.DefaultWeight);
                    break;

                case "dual_star":
                    var relays = dronesInHive.Where(d => d.Type == DroneType.Relay).Take(2).ToList();
                    if (relays.Count < 2 && dronesInHive.Count >= 2)
                    {
                        relays = dronesInHive.Take(2).ToList();
                    }
                    if (relays.Count < 2)
                    {
                        response.ErrorMessage = "Dual-star topology requires at least 2 drones.";
                        return response;
                    }
                    connectionsCreated = _topologyBuilder.BuildDualStarTopology(dronesInHive, relays[0].Id, relays[1].Id, request.DefaultWeight);
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
                TopologyType = "entry_relays",
                Success = false
            };

            // Check if Hive exists
            var hive = _hiveRepository.GetById(request.HiveId);
            if (hive == null)
            {
                response.ErrorMessage = $"Hive {request.HiveId} not found.";
                return response;
            }

            // Get all drones in the Hive
            var dronesInHive = _droneRepository.GetByHiveId(request.HiveId).ToList();
            if (!dronesInHive.Any())
            {
                response.ErrorMessage = $"Hive {request.HiveId} has no drones.";
                return response;
            }

            // Find relay drones (entry points to HiveMind)
            var relayDrones = dronesInHive.Where(d => d.Type == DroneType.Relay).ToList();
            if (!relayDrones.Any())
            {
                response.ErrorMessage = "No relay drones found. Cannot connect to HiveMind.";
                return response;
            }
            var entryRelays = new List<string>();

            if (request.EntryRelayIds != null && request.EntryRelayIds.Any())
            {
                foreach (var relayId in request.EntryRelayIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                {
                    var relay = relayDrones.FirstOrDefault(d => d.Id == relayId);
                    if (relay == null)
                    {
                        response.ErrorMessage = $"Relay drone {relayId} not found in Hive {request.HiveId}.";
                        return response;
                    }
                    entryRelays.Add(relay.Id);
                }
            }

            if (!entryRelays.Any())
            {
                entryRelays.Add(relayDrones.First().Id);
                if (relayDrones.Count > 1)
                {
                    entryRelays.Add(relayDrones[1].Id);
                }
            }

            _hiveRepository.SetEntryRelays(request.HiveId, entryRelays);

            response.Success = true;
            response.ConnectionsCreated = 0;
            response.EntryRelays = entryRelays.ToList();
            _logger.LogInformation("Hive {HiveId} entry relays set to: {Relays}",
                request.HiveId, string.Join(", ", entryRelays));

            return response;
        }

        public SwarmConnectivityResponse AnalyzeSwarmConnectivity(string hiveId)
        {
            return _connectivityAnalyzer.AnalyzeSwarmConnectivity(hiveId);
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

            // Get previous weight before update
            var previousWeight = _connectionManager.GetConnectionWeight(request.FromDroneId, request.ToDroneId);
            if (previousWeight == null)
            {
                response.ErrorMessage = $"Connection from {request.FromDroneId} to {request.ToDroneId} does not exist.";
                return response;
            }

            response.PreviousWeight = previousWeight;

            if (request.NewWeight <= 0)
            {
                _connectionManager.RemoveConnection(request.FromDroneId, request.ToDroneId);
                _connectionManager.RemoveConnection(request.ToDroneId, request.FromDroneId);
                response.NewWeight = 0;
                response.Success = true;
                _logger.LogDebug("Connection removed due to zero weight: {FromDroneId} <-> {ToDroneId}", request.FromDroneId, request.ToDroneId);
                return response;
            }

            // Update connection weight
            var updated = _connectionManager.UpdateConnectionWeight(request.FromDroneId, request.ToDroneId, request.NewWeight);
            if (!updated)
            {
                response.ErrorMessage = $"Failed to update connection from {request.FromDroneId} to {request.ToDroneId}.";
                return response;
            }

            response.Success = true;
            response.NewWeight = request.NewWeight;
            _logger.LogDebug("Connection degraded: {FromDroneId} -> {ToDroneId}, weight changed from {OldWeight} to {NewWeight}",
                request.FromDroneId, request.ToDroneId, previousWeight, request.NewWeight);

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

    }
}

