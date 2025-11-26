namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for batch drone join operation
    /// </summary>
    public class BatchJoinDronesResponse
    {
        /// <summary>
        /// The Hive ID
        /// </summary>
        public string HiveId { get; set; } = string.Empty;

        /// <summary>
        /// Total number of drones in the request
        /// </summary>
        public int TotalRequested { get; set; }

        /// <summary>
        /// Number of drones successfully added to the Hive
        /// </summary>
        public int Joined { get; set; }

        /// <summary>
        /// Number of drones that were already in the Hive
        /// </summary>
        public int AlreadyInHive { get; set; }

        /// <summary>
        /// Number of drones that failed to join
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// List of successfully joined drone IDs
        /// </summary>
        public IReadOnlyCollection<string> JoinedDroneIds { get; set; } = Array.Empty<string>();

        /// <summary>
        /// List of drone IDs that were already in the Hive
        /// </summary>
        public IReadOnlyCollection<string> AlreadyInHiveDroneIds { get; set; } = Array.Empty<string>();

        /// <summary>
        /// List of errors with details (drone ID and error message)
        /// </summary>
        public IReadOnlyCollection<BatchJoinError> Errors { get; set; } = Array.Empty<BatchJoinError>();
    }

    /// <summary>
    /// Error details for a failed drone join
    /// </summary>
    public class BatchJoinError
    {
        /// <summary>
        /// The drone ID that failed
        /// </summary>
        public string DroneId { get; set; } = string.Empty;

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

