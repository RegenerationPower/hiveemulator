using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
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
        public string Message { get; set; }
        public string? HiveId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

