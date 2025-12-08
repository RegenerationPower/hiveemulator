#nullable enable
using Models = DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Service for building network topologies
    /// </summary>
    public interface ITopologyBuilder
    {
        int BuildMeshTopology(IReadOnlyCollection<Models.Drone> drones, double defaultWeight);
        int BuildStarTopology(IReadOnlyCollection<Models.Drone> drones, string hubDroneId, double defaultWeight);
        int BuildDualStarTopology(IReadOnlyCollection<Models.Drone> drones, string hub1Id, string hub2Id, double defaultWeight);
        int RemoveAllConnections(IReadOnlyCollection<Models.Drone> drones);
    }
}

