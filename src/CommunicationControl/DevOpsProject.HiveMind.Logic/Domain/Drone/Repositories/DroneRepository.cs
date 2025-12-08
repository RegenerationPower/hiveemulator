#nullable enable
using DevOpsProject.HiveMind.Logic.State;
using Models = DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories
{
    /// <summary>
    /// In-memory implementation of drone repository
    /// </summary>
    public class DroneRepository : IDroneRepository
    {
        public IReadOnlyCollection<Models.Drone> GetAll() => HiveInMemoryState.Drones;

        public Models.Drone? GetById(string droneId) => HiveInMemoryState.GetDrone(droneId);

        public bool Exists(string droneId) => HiveInMemoryState.GetDrone(droneId) != null;

        public bool Add(Models.Drone drone)
        {
            var isNew = HiveInMemoryState.UpsertDrone(drone);
            return isNew;
        }

        public bool Update(Models.Drone drone)
        {
            var isNew = HiveInMemoryState.UpsertDrone(drone);
            return !isNew;
        }

        public bool Remove(string droneId)
        {
            HiveInMemoryState.RemoveDroneFromHive(droneId);
            HiveInMemoryState.RemoveDroneCommands(droneId);
            return HiveInMemoryState.RemoveDrone(droneId);
        }

        public IReadOnlyCollection<Models.Drone> GetByHiveId(string hiveId)
        {
            var droneIds = HiveInMemoryState.GetHiveDrones(hiveId);
            return HiveInMemoryState.Drones
                .Where(d => droneIds.Contains(d.Id))
                .ToList();
        }
    }
}

