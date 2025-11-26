namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Response for topology rebuild operation
    /// </summary>
    public class TopologyRebuildResponse
    {
        /// <summary>
        /// Whether the topology rebuild was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The Hive ID
        /// </summary>
        public string HiveId { get; set; }

        /// <summary>
        /// Topology type that was applied
        /// </summary>
        public string TopologyType { get; set; }

        /// <summary>
        /// Number of connections created
        /// </summary>
        public int ConnectionsCreated { get; set; }

        /// <summary>
        /// Number of connections removed
        /// </summary>
        public int ConnectionsRemoved { get; set; }

        /// <summary>
        /// Entry relay IDs registered during operations such as connect-hivemind.
        /// </summary>
        public IReadOnlyCollection<string> EntryRelays { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Error message if rebuild failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

