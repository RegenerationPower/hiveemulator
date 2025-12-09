using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.Drone;
using DevOpsProject.Shared.Models.DTO.Topology;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IDroneRelayService
    {
        IReadOnlyCollection<Drone> GetSwarm();
        bool UpsertDrone(Drone drone);
        BatchCreateDronesResponse BatchCreateDrones(BatchCreateDronesRequest request);
        bool RemoveDrone(string droneId);
        int RemoveAllDrones();
        DroneConnectionAnalysisResponse AnalyzeConnection(string droneId, double minimumWeight = 0.5);
        MeshCommandResponse SendCommandViaMesh(string targetDroneId, DroneCommand command, double minimumWeight = 0.5);
        TopologyRebuildResponse RebuildTopology(TopologyRebuildRequest request);
        TopologyRebuildResponse ConnectToHiveMind(ConnectToHiveMindRequest request);
        SwarmConnectivityResponse AnalyzeSwarmConnectivity(string hiveId);
        DegradeConnectionResponse DegradeConnection(DegradeConnectionRequest request);
        BatchDegradeConnectionsResponse BatchDegradeConnections(BatchDegradeConnectionsRequest request);
    }
}

