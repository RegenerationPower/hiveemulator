namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for batch connection degradation operation
    /// </summary>
    public class BatchDegradeConnectionsResponse
    {
        /// <summary>
        /// Total number of connections in the request
        /// </summary>
        public int TotalRequested { get; set; }

        /// <summary>
        /// Number of connections successfully degraded
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Number of connections that failed to degrade
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// List of successful degradation results
        /// </summary>
        public IReadOnlyCollection<DegradeConnectionResponse> Successful { get; set; } = Array.Empty<DegradeConnectionResponse>();

        /// <summary>
        /// List of failed degradation results with error messages
        /// </summary>
        public IReadOnlyCollection<DegradeConnectionResponse> FailedResults { get; set; } = Array.Empty<DegradeConnectionResponse>();
    }
}

