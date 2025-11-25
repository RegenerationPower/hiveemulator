using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Models.Commands.Drone
{
    public class DroneCommand
    {
        public Guid CommandId { get; set; }
        public string TargetDroneId { get; set; }
        public DroneCommandType CommandType { get; set; }
        public DateTime Timestamp { get; set; }
        public object? CommandPayload { get; set; }
    }

    public class MoveDroneCommandPayload
    {
        public float Lat { get; set; }
        public float Lon { get; set; }
        public float Height { get; set; }
    }

    public class ChangeConnectionDroneCommandPayload
    {
        public string TargetDroneId { get; set; }
        public double Weight { get; set; }
        public bool AddConnection { get; set; }
    }
}

