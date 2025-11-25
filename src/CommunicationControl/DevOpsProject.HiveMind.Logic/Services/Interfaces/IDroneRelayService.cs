using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.hive;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IDroneRelayService
    {
        IReadOnlyCollection<Drone> GetSwarm();
        bool UpsertDrone(Drone drone);
        bool RemoveDrone(string droneId);
        DroneConnectionAnalysisResponse AnalyzeConnection(string droneId, double minimumWeight = 0.5);
        MeshCommandResponse SendCommandViaMesh(string targetDroneId, DroneCommand command, double minimumWeight = 0.5);
        TopologyRebuildResponse RebuildTopology(TopologyRebuildRequest request);
        TopologyRebuildResponse ConnectToHiveMind(ConnectToHiveMindRequest request);
        SwarmConnectivityResponse AnalyzeSwarmConnectivity(string hiveId);
    }
}

