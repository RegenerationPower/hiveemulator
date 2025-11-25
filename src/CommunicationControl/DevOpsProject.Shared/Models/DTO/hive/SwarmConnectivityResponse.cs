namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for swarm connectivity analysis
    /// </summary>
    public class SwarmConnectivityResponse
    {
        /// <summary>
        /// The Hive ID
        /// </summary>
        public string HiveId { get; set; }

        /// <summary>
        /// Whether all drones in the swarm are connected
        /// </summary>
        public bool IsFullyConnected { get; set; }

        /// <summary>
        /// Number of connected components (isolated groups) in the swarm
        /// </summary>
        public int ConnectedComponents { get; set; }

        /// <summary>
        /// List of isolated drone groups (if any)
        /// </summary>
        public IReadOnlyCollection<IReadOnlyCollection<string>> IsolatedGroups { get; set; } = Array.Empty<IReadOnlyCollection<string>>();

        /// <summary>
        /// Total number of drones in the Hive
        /// </summary>
        public int TotalDrones { get; set; }

        /// <summary>
        /// Number of drones in the largest connected component
        /// </summary>
        public int LargestComponentSize { get; set; }
    }
}

