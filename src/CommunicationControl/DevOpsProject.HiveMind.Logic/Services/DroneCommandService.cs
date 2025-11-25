#nullable enable
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.hive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class DroneCommandService : IDroneCommandService
    {
        private readonly ILogger<DroneCommandService> _logger;
        private readonly IDroneRelayService _droneRelayService;
        private readonly HiveCommunicationConfig _hiveConfig;

        public DroneCommandService(ILogger<DroneCommandService> logger, IDroneRelayService droneRelayService, IOptionsSnapshot<HiveCommunicationConfig> hiveConfig)
        {
            _logger = logger;
            _droneRelayService = droneRelayService;
            _hiveConfig = hiveConfig.Value;
        }

        public DroneJoinResponse JoinDrone(string hiveId, DroneJoinRequest request)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                return new DroneJoinResponse
                {
                    Success = false,
                    Message = "Invalid Hive ID",
                    Timestamp = DateTime.UtcNow
                };
            }

            if (request == null || string.IsNullOrWhiteSpace(request.DroneId))
            {
                return new DroneJoinResponse
                {
                    Success = false,
                    Message = "Invalid drone ID",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Check if Hive exists
            var hive = HiveInMemoryState.GetHive(hiveId);
            if (hive == null)
            {
                return new DroneJoinResponse
                {
                    Success = false,
                    Message = $"Hive {hiveId} does not exist. Please create the Hive first.",
                    Timestamp = DateTime.UtcNow
                };
            }

            var existingDrone = HiveInMemoryState.GetDrone(request.DroneId);
            if (existingDrone == null)
            {
                return new DroneJoinResponse
                {
                    Success = false,
                    Message = $"Drone {request.DroneId} is not registered in the swarm. Please register the drone first.",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Check if drone is already in another hive
            var currentHiveId = HiveInMemoryState.GetDroneHive(request.DroneId);
            if (currentHiveId != null && currentHiveId != hiveId)
            {
                return new DroneJoinResponse
                {
                    Success = false,
                    Message = $"Drone {request.DroneId} is already connected to Hive {currentHiveId}. Cannot join Hive {hiveId}.",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Add drone to this hive
            bool added = HiveInMemoryState.AddDroneToHive(hiveId, request.DroneId);
            if (!added && currentHiveId == hiveId)
            {
                // Already in this hive
                _logger.LogInformation("Drone {DroneId} ({DroneName}) is already in Hive {HiveId}", 
                    request.DroneId, request.DroneName ?? "Unnamed", hiveId);
                return new DroneJoinResponse
                {
                    Success = true,
                    Message = $"Drone {request.DroneId} is already connected to Hive {hiveId}",
                    HiveId = hiveId,
                    Timestamp = DateTime.UtcNow
                };
            }

            _logger.LogInformation("Drone {DroneId} ({DroneName}) successfully joined Hive {HiveId}", 
                request.DroneId, request.DroneName ?? "Unnamed", hiveId);

            return new DroneJoinResponse
            {
                Success = true,
                Message = $"Drone {request.DroneId} successfully joined Hive {hiveId}",
                HiveId = hiveId,
                Timestamp = DateTime.UtcNow
            };
        }

        public IReadOnlyCollection<Drone> GetConnectedDrones(string hiveId, string droneId)
        {
            var drone = HiveInMemoryState.GetDrone(droneId);
            if (drone == null)
            {
                _logger.LogWarning("Requested connected drones for unknown drone {DroneId}", droneId);
                return Array.Empty<Drone>();
            }

            // Get drones from the same hive
            var hiveDrones = HiveInMemoryState.GetHiveDrones(hiveId);
            var connectedDroneIds = drone.Connections
                .Select(c => c.TargetDroneId)
                .ToHashSet();

            // Filter: only drones that are both connected (in connections) and in the same hive
            var connectedDrones = hiveDrones
                .Where(id => connectedDroneIds.Contains(id))
                .Select(id => HiveInMemoryState.GetDrone(id))
                .Where(d => d != null)
                .ToList()!;

            _logger.LogInformation("Drone {DroneId} requested connected drones info from Hive {HiveId}. Found {Count} connected drones.",
                droneId, hiveId, connectedDrones.Count);

            return connectedDrones;
        }

        public IReadOnlyCollection<Drone> GetHiveDrones(string hiveId)
        {
            var hiveDroneIds = HiveInMemoryState.GetHiveDrones(hiveId);
            var drones = hiveDroneIds
                .Select(id => HiveInMemoryState.GetDrone(id))
                .Where(d => d != null)
                .ToList()!;

            _logger.LogInformation("Requested drones for Hive {HiveId}. Found {Count} drones.", hiveId, drones.Count);
            return drones;
        }

        public DroneCommand? GetNextCommand(string droneId)
        {
            // Check if drone is in a Hive
            var hiveId = HiveInMemoryState.GetDroneHive(droneId);
            if (hiveId != null)
            {
                _logger.LogWarning("Drone {DroneId} is in Hive {HiveId} and cannot retrieve individual commands.", droneId, hiveId);
                return null;
            }

            var command = HiveInMemoryState.GetNextDroneCommand(droneId);
            if (command != null)
            {
                _logger.LogInformation("Drone {DroneId} retrieved command {CommandId} of type {CommandType}",
                    droneId, command.CommandId, command.CommandType);
            }
            return command;
        }

        public IReadOnlyCollection<DroneCommand> GetAllCommands(string droneId)
        {
            var commands = HiveInMemoryState.GetAllDroneCommands(droneId);
            var hiveId = HiveInMemoryState.GetDroneHive(droneId);
            if (hiveId != null)
            {
                _logger.LogInformation("Drone {DroneId} in Hive {HiveId} requested all commands. Found {Count} commands.", droneId, hiveId, commands.Count);
            }
            else
            {
                _logger.LogInformation("Drone {DroneId} requested all commands. Found {Count} commands.", droneId, commands.Count);
            }
            return commands;
        }

        public void SendCommand(DroneCommand command)
        {
            if (command == null)
            {
                _logger.LogWarning("Attempted to send null command");
                return;
            }

            if (string.IsNullOrWhiteSpace(command.TargetDroneId))
            {
                _logger.LogWarning("Attempted to send command with invalid target drone ID");
                return;
            }

            var targetDrone = HiveInMemoryState.GetDrone(command.TargetDroneId);
            if (targetDrone == null)
            {
                _logger.LogWarning("Attempted to send command to unknown drone {DroneId}", command.TargetDroneId);
                return;
            }

            // Check if drone is in a Hive
            var hiveId = HiveInMemoryState.GetDroneHive(command.TargetDroneId);
            if (hiveId != null)
            {
                _logger.LogWarning("Cannot send individual command to drone {DroneId} because it is in Hive {HiveId}. Use Hive command endpoint instead.", 
                    command.TargetDroneId, hiveId);
                return;
            }

            if (command.CommandId == Guid.Empty)
            {
                command.CommandId = Guid.NewGuid();
            }

            if (command.Timestamp == default)
            {
                command.Timestamp = DateTime.UtcNow;
            }

            HiveInMemoryState.AddDroneCommand(command);
            _logger.LogInformation("Command {CommandId} of type {CommandType} sent to drone {DroneId}",
                command.CommandId, command.CommandType, command.TargetDroneId);
        }

        public int SendCommandToHive(string hiveId, DroneCommand command)
        {
            if (command == null)
            {
                _logger.LogWarning("Attempted to send null command to Hive");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(hiveId))
            {
                _logger.LogWarning("Attempted to send command to Hive with invalid Hive ID");
                return 0;
            }

            // Check if Hive exists
            var hive = HiveInMemoryState.GetHive(hiveId);
            if (hive == null)
            {
                _logger.LogWarning("Attempted to send command to non-existent Hive {HiveId}", hiveId);
                return 0;
            }

            // Get all drones in the Hive
            var hiveDrones = HiveInMemoryState.GetHiveDrones(hiveId);
            if (!hiveDrones.Any())
            {
                _logger.LogWarning("Hive {HiveId} has no drones. Command not sent.", hiveId);
                return 0;
            }

            // Auto-generate command ID and timestamp if not provided
            if (command.CommandId == Guid.Empty)
            {
                command.CommandId = Guid.NewGuid();
            }

            if (command.Timestamp == default)
            {
                command.Timestamp = DateTime.UtcNow;
            }

            // Set commandPayload to null for commands that don't need it
            if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
            {
                command.CommandPayload = null;
            }

            int sentCount = 0;
            foreach (var droneId in hiveDrones)
            {
                // Clear individual commands for this drone
                HiveInMemoryState.ClearDroneCommands(droneId);

                // Create a copy of the command for each drone
                var droneCommand = new DroneCommand
                {
                    CommandId = Guid.NewGuid(), // Each drone gets unique command ID
                    TargetDroneId = droneId,
                    CommandType = command.CommandType,
                    Timestamp = command.Timestamp,
                    CommandPayload = command.CommandPayload
                };

                HiveInMemoryState.AddDroneCommand(droneCommand);
                sentCount++;
            }

            _logger.LogInformation("Command {CommandId} of type {CommandType} sent to Hive {HiveId}. Sent to {Count} drones. Individual commands cleared.",
                command.CommandId, command.CommandType, hiveId, sentCount);

            return sentCount;
        }
    }
}

