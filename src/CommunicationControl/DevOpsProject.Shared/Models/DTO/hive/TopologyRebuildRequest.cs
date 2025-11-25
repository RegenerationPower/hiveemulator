namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to rebuild topology between drones
    /// </summary>
    public class TopologyRebuildRequest
    {
        /// <summary>
        /// The Hive ID where topology should be rebuilt
        /// </summary>
        public string HiveId { get; set; }

        /// <summary>
        /// Topology type: "star", "dual_star", or "mesh"
        /// </summary>
        public string TopologyType { get; set; }

        /// <summary>
        /// Default connection weight for new connections (default: 0.8)
        /// </summary>
        public double DefaultWeight { get; set; } = 0.8;
    }
}

