namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to degrade multiple connections at once
    /// </summary>
    public class BatchDegradeConnectionsRequest
    {
        /// <summary>
        /// List of connection degradation requests
        /// </summary>
        public List<DegradeConnectionRequest> Connections { get; set; } = new();
    }
}

