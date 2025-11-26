namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to add multiple drones to a Hive at once
    /// </summary>
    public class BatchJoinDronesRequest
    {
        /// <summary>
        /// List of drone IDs to add to the Hive
        /// </summary>
        public List<string> DroneIds { get; set; } = new();
    }
}

