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
}

