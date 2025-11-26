using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to create multiple drones at once
    /// </summary>
    public class BatchCreateDronesRequest
    {
        /// <summary>
        /// List of drones to create
        /// </summary>
        public List<Drone> Drones { get; set; } = new();
    }
}

