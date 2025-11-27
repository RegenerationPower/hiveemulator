using System.ComponentModel.DataAnnotations;
using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models.DTO.hive
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

    public class HiveTelemetryResponse
    {
        public DateTime Timestamp { get; set; }
    }
}
