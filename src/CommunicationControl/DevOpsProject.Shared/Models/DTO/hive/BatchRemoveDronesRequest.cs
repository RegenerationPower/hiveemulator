using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class BatchRemoveDronesRequest
    {
        [Required]
        public IReadOnlyCollection<string> DroneIds { get; set; } = new List<string>();
    }
}

