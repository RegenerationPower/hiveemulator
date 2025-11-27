using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.DTO.hive;

namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces
{
    public interface IHiveMindMeshIntegrationService
    {
        Task<Hive?> CreateHiveAsync(HiveCreateRequest request);
        Task<BatchCreateDronesResponse?> BatchCreateDronesAsync(BatchCreateDronesRequest request);
        Task<BatchJoinDronesResponse?> BatchJoinDronesAsync(string hiveId, BatchJoinDronesRequest request);
        Task<TopologyRebuildResponse?> RebuildTopologyAsync(string hiveId, TopologyRebuildRequest request);
        Task<TopologyRebuildResponse?> ConnectHiveMindAsync(string hiveId, ConnectToHiveMindRequest request);
        Task<SwarmConnectivityResponse?> GetConnectivityAsync(string hiveId);
        Task<DegradeConnectionResponse?> DegradeConnectionAsync(DegradeConnectionRequest request);
        Task<BatchDegradeConnectionsResponse?> BatchDegradeConnectionsAsync(BatchDegradeConnectionsRequest request);
        Task LogConnectivitySnapshotAsync(string hiveId);
    }
}

