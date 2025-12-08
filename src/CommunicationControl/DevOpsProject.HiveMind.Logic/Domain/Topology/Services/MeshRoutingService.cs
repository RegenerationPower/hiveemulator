#nullable enable
using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Topology.Services;
using Models = DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.Topology;
using DevOpsProject.HiveMind.Logic.State;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Service for routing commands through mesh network
    /// </summary>
    public class MeshRoutingService
    {
        private readonly IDroneRepository _droneRepository;
        private readonly IConnectivityAnalyzer _connectivityAnalyzer;
        private readonly ILogger<MeshRoutingService> _logger;

        public MeshRoutingService(
            IDroneRepository droneRepository,
            IConnectivityAnalyzer connectivityAnalyzer,
            ILogger<MeshRoutingService> logger)
        {
            _droneRepository = droneRepository;
            _connectivityAnalyzer = connectivityAnalyzer;
            _logger = logger;
        }

        public MeshCommandResponse RouteCommand(string targetDroneId, DroneCommand command, double minimumWeight = 0.5)
        {
            var response = new MeshCommandResponse
            {
                TargetDroneId = targetDroneId,
                RoutePath = Array.Empty<string>(),
                Success = false
            };

            var analysis = _connectivityAnalyzer.AnalyzeConnection(targetDroneId, minimumWeight);

            if (!analysis.CanConnect || !analysis.Path.Any())
            {
                response.ErrorMessage = $"Cannot reach drone {targetDroneId} through mesh network. No valid route found.";
                _logger.LogWarning("Cannot send command to {DroneId} via mesh: {Error}", targetDroneId, response.ErrorMessage);
                return response;
            }

            var routePath = analysis.Path.ToList();
            var routeWeights = analysis.PathWeights?.ToList() ?? new List<double>();

            response.RoutePath = routePath;
            response.RouteWeights = routeWeights;
            response.MinimumLinkWeight = analysis.MinimumLinkWeight;
            response.HopCount = analysis.HopCount;

            LogRoute(routePath, routeWeights, targetDroneId);

            if (routePath.Count <= 1)
            {
                return SendDirectCommand(targetDroneId, command, response);
            }

            return SendViaRelays(routePath, targetDroneId, command, response);
        }

        private MeshCommandResponse SendDirectCommand(string targetDroneId, DroneCommand command, MeshCommandResponse response)
        {
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

        private MeshCommandResponse SendViaRelays(
            List<string> routePath,
            string targetDroneId,
            DroneCommand command,
            MeshCommandResponse response)
        {
            int relaysUsed = 0;

            for (int i = 0; i < routePath.Count - 1; i++)
            {
                var currentDroneId = routePath[i];
                var nextDroneId = routePath[i + 1];
                var isFinalHop = (i == routePath.Count - 2);

                var relayCommand = CreateRelayCommand(currentDroneId, targetDroneId, nextDroneId, isFinalHop, command, routePath);
                HiveInMemoryState.AddDroneCommand(relayCommand);
                relaysUsed++;

                _logger.LogInformation("Relay command sent to {CurrentDroneId} for routing to {TargetDroneId} via {NextDroneId}",
                    currentDroneId, targetDroneId, nextDroneId);
            }

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

        private static DroneCommand CreateRelayCommand(
            string currentDroneId,
            string targetDroneId,
            string? nextDroneId,
            bool isFinalHop,
            DroneCommand finalCommand,
            List<string> routePath)
        {
            return new DroneCommand
            {
                CommandId = Guid.NewGuid(),
                TargetDroneId = currentDroneId,
                CommandType = DroneCommandType.Relay,
                Timestamp = DateTime.UtcNow,
                CommandPayload = new RelayDroneCommandPayload
                {
                    FinalDestinationDroneId = targetDroneId,
                    NextHopDroneId = isFinalHop ? null : nextDroneId,
                    FinalCommand = finalCommand,
                    RoutePath = routePath
                }
            };
        }

        private void LogRoute(List<string> routePath, List<double> routeWeights, string targetDroneId)
        {
            if (routePath.Count > 1 && routeWeights.Any())
            {
                var segments = new List<string>();
                for (int i = 0; i < routePath.Count - 1; i++)
                {
                    var weight = i < routeWeights.Count ? routeWeights[i] : 0;
                    segments.Add($"{routePath[i]}->{routePath[i + 1]}({weight:F2})");
                }
                _logger.LogInformation("Mesh route for {TargetDroneId}: {Segments}",
                    targetDroneId, string.Join(" | ", segments));
            }
        }
    }
}

