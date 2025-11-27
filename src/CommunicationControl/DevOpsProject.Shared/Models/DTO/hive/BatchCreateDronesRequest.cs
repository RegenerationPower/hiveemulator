using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class BatchCreateDronesRequest
    {
        [Required]
        public List<Drone> Drones { get; set; } = new();
    }

    public class BatchCreateDronesResponse
    {
        public int TotalRequested { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }
        public IReadOnlyCollection<string> CreatedDroneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> UpdatedDroneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
    }
}

