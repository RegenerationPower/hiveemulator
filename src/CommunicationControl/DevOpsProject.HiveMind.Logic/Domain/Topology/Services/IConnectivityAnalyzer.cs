#nullable enable
using DevOpsProject.Shared.Models.DTO.Topology;

namespace DevOpsProject.HiveMind.Logic.Domain.Topology.Services
{
    /// <summary>
    /// Service for analyzing network connectivity
    /// </summary>
    public interface IConnectivityAnalyzer
    {
        DroneConnectionAnalysisResponse AnalyzeConnection(string targetDroneId, double minimumWeight = 0.5);
        SwarmConnectivityResponse AnalyzeSwarmConnectivity(string hiveId);
    }
}

