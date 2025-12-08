#nullable enable
using Models = DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Service for managing individual connections between drones
    /// </summary>
    public interface IConnectionManager
    {
        bool UpdateConnectionWeight(string fromDroneId, string toDroneId, double newWeight);
        bool RemoveConnection(string fromDroneId, string toDroneId);
        double? GetConnectionWeight(string fromDroneId, string toDroneId);
    }
}

