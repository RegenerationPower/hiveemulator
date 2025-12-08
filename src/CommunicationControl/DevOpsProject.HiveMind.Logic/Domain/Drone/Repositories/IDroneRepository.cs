#nullable enable
using Models = DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories
{
    /// <summary>
    /// Repository for drone persistence operations
    /// </summary>
    public interface IDroneRepository
    {
        IReadOnlyCollection<Models.Drone> GetAll();
        Models.Drone? GetById(string droneId);
        bool Exists(string droneId);
        bool Add(Models.Drone drone);
        bool Update(Models.Drone drone);
        bool Remove(string droneId);
        IReadOnlyCollection<Models.Drone> GetByHiveId(string hiveId);
    }
}

