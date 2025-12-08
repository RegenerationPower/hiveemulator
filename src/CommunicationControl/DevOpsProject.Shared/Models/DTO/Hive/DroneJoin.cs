#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.Hive
{
    public class DroneJoinRequest
    {
        [Required]
        public string DroneId { get; set; } = string.Empty;
        public string? DroneName { get; set; }
    }

    public class DroneJoinResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? HiveId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

