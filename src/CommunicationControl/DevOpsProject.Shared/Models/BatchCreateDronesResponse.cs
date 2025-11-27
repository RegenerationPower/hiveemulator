namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for batch drone creation
    /// </summary>
    public class BatchCreateDronesResponse
    {
        /// <summary>
        /// Total number of drones in the request
        /// </summary>
        public int TotalRequested { get; set; }

        /// <summary>
        /// Number of drones successfully created (new)
        /// </summary>
        public int Created { get; set; }

        /// <summary>
        /// Number of drones updated (already existed)
        /// </summary>
        public int Updated { get; set; }

        /// <summary>
        /// Number of drones that failed to create/update
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// List of successfully created drone IDs
        /// </summary>
        public IReadOnlyCollection<string> CreatedDroneIds { get; set; } = Array.Empty<string>();

        /// <summary>
        /// List of updated drone IDs
        /// </summary>
        public IReadOnlyCollection<string> UpdatedDroneIds { get; set; } = Array.Empty<string>();

        /// <summary>
        /// List of errors (if any)
        /// </summary>
        public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
    }
}

