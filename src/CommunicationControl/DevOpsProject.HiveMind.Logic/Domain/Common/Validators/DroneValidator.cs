#nullable enable
using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Domain.Common.Validators
{
    /// <summary>
    /// Validator for Drone-related operations
    /// </summary>
    public class DroneValidator
    {
        private readonly IDroneRepository _droneRepository;
        private readonly IHiveRepository _hiveRepository;
        private readonly ILogger<DroneValidator> _logger;

        public DroneValidator(
            IDroneRepository droneRepository,
            IHiveRepository hiveRepository,
            ILogger<DroneValidator> logger)
        {
            _droneRepository = droneRepository;
            _hiveRepository = hiveRepository;
            _logger = logger;
        }

        public ValidationResult ValidateDroneExists(string droneId)
        {
            if (string.IsNullOrWhiteSpace(droneId))
            {
                return ValidationResult.Failure("Drone ID cannot be null or empty");
            }

            if (!_droneRepository.Exists(droneId))
            {
                _logger.LogWarning("Drone {DroneId} is not registered in the swarm", droneId);
                return ValidationResult.Failure($"Drone {droneId} is not registered in the swarm. Please register the drone first.");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateDroneNotInAnotherHive(string droneId, string targetHiveId)
        {
            var currentHiveId = GetDroneHiveId(droneId);
            if (currentHiveId != null && currentHiveId != targetHiveId)
            {
                _logger.LogWarning("Drone {DroneId} is already in Hive {CurrentHiveId}, cannot join {TargetHiveId}",
                    droneId, currentHiveId, targetHiveId);
                return ValidationResult.Failure($"Drone {droneId} is already connected to Hive {currentHiveId}. Cannot join Hive {targetHiveId}.");
            }

            return ValidationResult.Success();
        }

        private string? GetDroneHiveId(string droneId)
        {
            var allHives = _hiveRepository.GetAll();
            foreach (var hive in allHives)
            {
                var droneIds = _hiveRepository.GetDroneIds(hive.Id);
                if (droneIds.Contains(droneId))
                {
                    return hive.Id;
                }
            }
            return null;
        }
    }
}

