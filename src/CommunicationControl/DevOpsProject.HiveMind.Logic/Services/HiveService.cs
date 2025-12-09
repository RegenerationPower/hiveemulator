#nullable enable
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class HiveService : IHiveService
    {
        private readonly ILogger<HiveService> _logger;

        public HiveService(ILogger<HiveService> logger)
        {
            _logger = logger;
        }

        public Hive CreateHive(string hiveId, string? name = null)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                throw new ArgumentException("Hive ID cannot be null or empty", nameof(hiveId));
            }

            var existingHive = HiveInMemoryState.GetHive(hiveId);
            if (existingHive != null)
            {
                throw new InvalidOperationException($"Hive with ID '{hiveId}' already exists");
            }

            var hive = new Hive
            {
                Id = hiveId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            HiveInMemoryState.AddHive(hive);
            _logger.LogInformation("Hive {HiveId} ({HiveName}) created successfully", hiveId, name ?? "Unnamed");
            
            return hive;
        }

        public bool DeleteHive(string hiveId)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                _logger.LogWarning("Attempted to delete Hive with null or empty ID");
                return false;
            }

            // Remove all drones from this hive first
            var hiveDrones = HiveInMemoryState.GetHiveDrones(hiveId);
            foreach (var droneId in hiveDrones)
            {
                HiveInMemoryState.RemoveDroneFromHive(droneId);
            }

            var deleted = HiveInMemoryState.RemoveHive(hiveId);
            if (deleted)
            {
                _logger.LogInformation("Hive {HiveId} and all its drones removed successfully", hiveId);
            }
            else
            {
                _logger.LogWarning("Attempted to delete Hive {HiveId}, but it was not found", hiveId);
            }

            return deleted;
        }

        public Hive? GetHive(string hiveId)
        {
            return HiveInMemoryState.GetHive(hiveId);
        }

        public IReadOnlyCollection<Hive> GetAllHives()
        {
            return HiveInMemoryState.GetAllHives();
        }

        public int DeleteAllHives()
        {
            var allHives = HiveInMemoryState.GetAllHives();
            int deletedCount = 0;

            foreach (var hive in allHives)
            {
                if (DeleteHive(hive.Id))
                {
                    deletedCount++;
                }
            }

            _logger.LogInformation("Deleted all {Count} hives", deletedCount);
            return deletedCount;
        }
    }
}

