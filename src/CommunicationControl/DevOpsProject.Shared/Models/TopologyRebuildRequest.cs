using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class TopologyRebuildRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;

        [Required]
        public string TopologyType { get; set; } = string.Empty;

        public double DefaultWeight { get; set; } = 0.8;
    }
}

