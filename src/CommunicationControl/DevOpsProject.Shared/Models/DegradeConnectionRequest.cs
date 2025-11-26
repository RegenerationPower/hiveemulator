namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to degrade (change weight of) a connection between two drones
    /// </summary>
    public class DegradeConnectionRequest
    {
        /// <summary>
        /// Source drone ID
        /// </summary>
        public string FromDroneId { get; set; } = string.Empty;

        /// <summary>
        /// Target drone ID
        /// </summary>
        public string ToDroneId { get; set; } = string.Empty;

        /// <summary>
        /// New connection weight (0.0 to 1.0). Lower values indicate degraded connection.
        /// </summary>
        public double NewWeight { get; set; }
    }
}

