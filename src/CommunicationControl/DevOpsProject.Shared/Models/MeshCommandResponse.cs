namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for mesh command delivery
    /// </summary>
    public class MeshCommandResponse
    {
        /// <summary>
        /// Whether the command was successfully sent through the mesh network
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The target drone ID
        /// </summary>
        public string TargetDroneId { get; set; }

        /// <summary>
        /// The route path used for delivery
        /// </summary>
        public IReadOnlyCollection<string> RoutePath { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Weights of each connection along the route (aligned with RoutePath segments)
        /// </summary>
        public IReadOnlyCollection<double> RouteWeights { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Minimum link weight on the route (quality of connection)
        /// </summary>
        public double MinimumLinkWeight { get; set; }

        /// <summary>
        /// Number of hops in the route
        /// </summary>
        public int HopCount { get; set; }

        /// <summary>
        /// Number of relay drones that received the command
        /// </summary>
        public int RelaysUsed { get; set; }

        /// <summary>
        /// Error message if delivery failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

