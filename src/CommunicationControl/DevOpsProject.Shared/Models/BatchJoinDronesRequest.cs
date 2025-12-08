using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class BatchJoinDronesRequest
    {
        [Required]
        public List<string> DroneIds { get; set; } = new();
    }
}

