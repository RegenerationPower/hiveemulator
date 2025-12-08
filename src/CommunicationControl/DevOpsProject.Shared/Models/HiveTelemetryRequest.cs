using DevOpsProject.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models
{
    public class HiveTelemetryRequest
    {
        [Required]
        public string HiveID { get; set; }
        public Location Location { get; set; }
        public float Speed { get; set; }
        public float Height { get; set; }
        public HiveMindState State { get; set; }
    }
}
