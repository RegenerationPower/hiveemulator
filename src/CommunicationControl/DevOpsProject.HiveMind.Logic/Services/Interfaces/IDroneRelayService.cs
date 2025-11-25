using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.DTO.hive;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IDroneRelayService
    {
        IReadOnlyCollection<Drone> GetSwarm();
        bool UpsertDrone(Drone drone);
        bool RemoveDrone(Guid droneId);
        DroneConnectionAnalysisResponse AnalyzeConnection(Guid droneId, double minimumWeight = 0.5);
    }
}

