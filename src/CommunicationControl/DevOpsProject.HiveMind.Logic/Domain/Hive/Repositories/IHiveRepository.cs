#nullable enable
using Models = DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories
{
    /// <summary>
    /// Repository for hive persistence operations
    /// </summary>
    public interface IHiveRepository
    {
        Models.Hive? GetById(string hiveId);
        IReadOnlyCollection<Models.Hive> GetAll();
        bool Exists(string hiveId);
        bool Add(Models.Hive hive);
        bool Remove(string hiveId);
        IReadOnlyCollection<string> GetDroneIds(string hiveId);
        bool AddDroneToHive(string hiveId, string droneId);
        bool RemoveDroneFromHive(string hiveId, string droneId);
        void SetEntryRelays(string hiveId, IReadOnlyCollection<string> entryRelayIds);
        IReadOnlyCollection<string> GetEntryRelays(string hiveId);
    }
}

