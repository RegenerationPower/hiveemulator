using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models.Commands.Drone;

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
        public List<DroneTelemetryInfo>? Drones { get; set; }
    }

    public class DroneTelemetryInfo
    {
        public string DroneId { get; set; } = string.Empty;
        public DroneType Type { get; set; }
        public int ConnectionCount { get; set; }
        public List<DroneCommand> IndividualCommands { get; set; } = new();
        public bool HasHiveCommand { get; set; }
    }
}

