#nullable enable
using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using Models = DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Manages individual connections between drones
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly IDroneRepository _droneRepository;
        private readonly ILogger<ConnectionManager> _logger;

        public ConnectionManager(IDroneRepository droneRepository, ILogger<ConnectionManager> logger)
        {
            _droneRepository = droneRepository;
            _logger = logger;
        }

        public bool UpdateConnectionWeight(string fromDroneId, string toDroneId, double newWeight)
        {
            var fromDrone = _droneRepository.GetById(fromDroneId);
            if (fromDrone == null)
            {
                _logger.LogWarning("Cannot update connection: source drone {FromDroneId} not found", fromDroneId);
                return false;
            }

            fromDrone.Connections ??= new List<Models.DroneConnection>();
            var connection = fromDrone.Connections.FirstOrDefault(c => c.TargetDroneId == toDroneId);
            
            if (connection == null)
            {
                _logger.LogWarning("Connection from {FromDroneId} to {ToDroneId} does not exist", fromDroneId, toDroneId);
                return false;
            }

            var previousWeight = connection.Weight;
            connection.Weight = newWeight;
            _droneRepository.Update(fromDrone);

            // Update reverse connection if it exists (bidirectional)
            var toDrone = _droneRepository.GetById(toDroneId);
            if (toDrone?.Connections != null)
            {
                var reverseConnection = toDrone.Connections.FirstOrDefault(c => c.TargetDroneId == fromDroneId);
                if (reverseConnection != null)
                {
                    reverseConnection.Weight = newWeight;
                    _droneRepository.Update(toDrone);
                }
            }

            _logger.LogDebug("Connection weight updated: {FromDroneId} -> {ToDroneId}, {OldWeight} -> {NewWeight}",
                fromDroneId, toDroneId, previousWeight, newWeight);

            return true;
        }

        public bool RemoveConnection(string fromDroneId, string toDroneId)
        {
            var fromDrone = _droneRepository.GetById(fromDroneId);
            if (fromDrone == null)
            {
                return false;
            }

            var removed = fromDrone.Connections?.RemoveAll(c => c.TargetDroneId == toDroneId) ?? 0;
            if (removed > 0)
            {
                _droneRepository.Update(fromDrone);
                _logger.LogDebug("Connection removed: {FromDroneId} -> {ToDroneId}", fromDroneId, toDroneId);
            }

            return removed > 0;
        }

        public double? GetConnectionWeight(string fromDroneId, string toDroneId)
        {
            var fromDrone = _droneRepository.GetById(fromDroneId);
            return fromDrone?.Connections?.FirstOrDefault(c => c.TargetDroneId == toDroneId)?.Weight;
        }
    }
}

