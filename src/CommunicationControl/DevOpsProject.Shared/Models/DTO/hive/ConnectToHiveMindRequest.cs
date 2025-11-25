namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to connect drones to HiveMind in star or dual-star topology
    /// </summary>
    public class ConnectToHiveMindRequest
    {
        /// <summary>
        /// The Hive ID
        /// </summary>
        public string HiveId { get; set; }

        /// <summary>
        /// Topology type: "star" or "dual_star"
        /// </summary>
        public string TopologyType { get; set; }

        /// <summary>
        /// Connection weight to HiveMind (default: 1.0)
        /// </summary>
        public double ConnectionWeight { get; set; } = 1.0;

        /// <summary>
        /// For dual-star: IDs of two relay drones to use as hubs (optional, will be auto-selected if not provided)
        /// </summary>
        public List<string>? HubDroneIds { get; set; }
    }
}

