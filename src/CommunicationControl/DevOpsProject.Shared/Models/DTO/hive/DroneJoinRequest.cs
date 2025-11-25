namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class DroneJoinRequest
    {
        public string DroneId { get; set; }
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

