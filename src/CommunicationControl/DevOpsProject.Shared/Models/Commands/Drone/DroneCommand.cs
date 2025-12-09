#nullable enable
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models.Commands.Drone
{
    public class DroneCommand
    {
        [JsonPropertyName("commandId")]
        public Guid CommandId { get; set; }

        [Required]
        [JsonPropertyName("targetDroneId")]
        public string TargetDroneId { get; set; } = string.Empty;

        [JsonPropertyName("commandType")]
        public DroneCommandType CommandType { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("commandPayload")]
        public object? CommandPayload { get; set; }
    }

    public class MoveDroneCommandPayload
    {
        [Required]
        [JsonPropertyName("lat")]
        public float Lat { get; set; }

        [Required]
        [JsonPropertyName("lon")]
        public float Lon { get; set; }

        [Required]
        [JsonPropertyName("height")]
        public float Height { get; set; }

        [Required]
        [JsonPropertyName("speed")]
        public float Speed { get; set; }
    }

    public class ChangeConnectionDroneCommandPayload
    {
        [Required]
        [JsonPropertyName("targetDroneId")]
        public string TargetDroneId { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("addConnection")]
        public bool AddConnection { get; set; }
    }
}

