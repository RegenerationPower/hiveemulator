using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IHiveService
    {
        Hive CreateHive(string hiveId, string? name = null);
        bool DeleteHive(string hiveId);
        Hive? GetHive(string hiveId);
        IReadOnlyCollection<Hive> GetAllHives();
    }
}

