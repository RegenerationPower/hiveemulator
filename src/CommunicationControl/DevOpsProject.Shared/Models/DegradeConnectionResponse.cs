namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for connection degradation operation
    /// </summary>
    public class DegradeConnectionResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Source drone ID
        /// </summary>
        public string FromDroneId { get; set; } = string.Empty;

        /// <summary>
        /// Target drone ID
        /// </summary>
        public string ToDroneId { get; set; } = string.Empty;

        /// <summary>
        /// Previous connection weight
        /// </summary>
        public double? PreviousWeight { get; set; }

        /// <summary>
        /// New connection weight
        /// </summary>
        public double NewWeight { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

