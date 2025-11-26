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

        /// <summary>
        /// Detailed information about each connected component
        /// </summary>
        public IReadOnlyCollection<ComponentInfo> Components { get; set; } = Array.Empty<ComponentInfo>();

        /// <summary>
        /// Total number of connections in the swarm
        /// </summary>
        public int TotalConnections { get; set; }

        /// <summary>
        /// Average connection weight
        /// </summary>
        public double AverageConnectionWeight { get; set; }

        /// <summary>
        /// Minimum connection weight
        /// </summary>
        public double MinimumConnectionWeight { get; set; }

        /// <summary>
        /// Maximum connection weight
        /// </summary>
        public double MaximumConnectionWeight { get; set; }

        /// <summary>
        /// Detailed connection graph (drone ID -> list of connected drones with weights)
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyCollection<ConnectionInfo>> ConnectionGraph { get; set; } = new Dictionary<string, IReadOnlyCollection<ConnectionInfo>>();
    }

    /// <summary>
    /// Information about a connected component
    /// </summary>
    public class ComponentInfo
    {
        /// <summary>
        /// Component ID (index)
        /// </summary>
        public int ComponentId { get; set; }

        /// <summary>
        /// Number of drones in this component
        /// </summary>
        public int DroneCount { get; set; }

        /// <summary>
        /// List of drone IDs in this component
        /// </summary>
        public IReadOnlyCollection<string> DroneIds { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Number of connections within this component
        /// </summary>
        public int ConnectionCount { get; set; }
    }

    /// <summary>
    /// Information about a connection
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Target drone ID
        /// </summary>
        public string TargetDroneId { get; set; } = string.Empty;

        /// <summary>
        /// Connection weight
        /// </summary>
        public double Weight { get; set; }
    }
}

