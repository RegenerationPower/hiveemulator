using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models
{
    public class HiveTelemetryModel
    {
        public string HiveID { get; set; }
        public Location Location { get; set; }
        public float Speed { get; set; }
        public float Height { get; set; }
        public HiveMindState State { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
