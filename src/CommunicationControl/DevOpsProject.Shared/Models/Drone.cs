using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models
{
    public class Drone
    {
        public string Id { get; set; }
        public DroneType Type { get; set; }
        public List<DroneConnection> Connections { get; set; } = new();
    }

    public class DroneConnection
    {
        public string TargetDroneId { get; set; }
        public double Weight { get; set; }
    }
}

