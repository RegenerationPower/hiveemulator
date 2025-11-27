#nullable enable
using System.ComponentModel.DataAnnotations;
using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models.Commands.Drone
{
    public class DroneCommand
    {
        public Guid CommandId { get; set; }

        [Required]
        public string TargetDroneId { get; set; } = string.Empty;

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
        [Required]
        public string TargetDroneId { get; set; } = string.Empty;

        public double Weight { get; set; }
        public bool AddConnection { get; set; }
    }
}

