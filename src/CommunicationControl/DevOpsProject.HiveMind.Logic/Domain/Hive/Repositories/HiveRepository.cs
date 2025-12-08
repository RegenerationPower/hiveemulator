#nullable enable
using DevOpsProject.HiveMind.Logic.State;
using Models = DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories
{
    /// <summary>
    /// In-memory implementation of hive repository
    /// </summary>
    public class HiveRepository : IHiveRepository
    {
        public Models.Hive? GetById(string hiveId) => HiveInMemoryState.GetHive(hiveId);

        public IReadOnlyCollection<Models.Hive> GetAll() => HiveInMemoryState.GetAllHives();

        public bool Exists(string hiveId) => HiveInMemoryState.GetHive(hiveId) != null;

        public bool Add(Models.Hive hive)
        {
            HiveInMemoryState.AddHive(hive);
            return true;
        }

        public bool Remove(string hiveId) => HiveInMemoryState.RemoveHive(hiveId);

        public IReadOnlyCollection<string> GetDroneIds(string hiveId) => 
            HiveInMemoryState.GetHiveDrones(hiveId);

        public bool AddDroneToHive(string hiveId, string droneId)
        {
            var result = HiveInMemoryState.AddDroneToHive(hiveId, droneId);
            return result == HiveMembershipResult.Added;
        }

        public bool RemoveDroneFromHive(string hiveId, string droneId) =>
            HiveInMemoryState.RemoveDroneFromHive(droneId);

        public void SetEntryRelays(string hiveId, IReadOnlyCollection<string> entryRelayIds) =>
            HiveInMemoryState.SetHiveEntryRelays(hiveId, entryRelayIds);

        public IReadOnlyCollection<string> GetEntryRelays(string hiveId) =>
            HiveInMemoryState.GetHiveEntryRelays(hiveId);
    }
}

