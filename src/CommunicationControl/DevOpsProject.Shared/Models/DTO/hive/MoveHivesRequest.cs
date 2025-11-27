using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class MoveHivesRequest
    {
        [Required]
        public List<string> Hives { get; set; } = new();

        [Required]
        public Location Destination { get; set; }
    }
}
