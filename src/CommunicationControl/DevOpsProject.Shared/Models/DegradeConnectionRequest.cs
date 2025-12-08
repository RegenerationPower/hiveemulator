using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class DegradeConnectionRequest
    {
        [Required]
        public string FromDroneId { get; set; } = string.Empty;

        [Required]
        public string ToDroneId { get; set; } = string.Empty;

        [Range(0.0, 1.0)]
        public double NewWeight { get; set; }
    }
}

