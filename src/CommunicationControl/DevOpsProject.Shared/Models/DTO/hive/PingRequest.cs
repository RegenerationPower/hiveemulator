namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class PingRequest
    {
        public DateTime Timestamp { get; set; }
        public string HiveID { get; set; }
    }

    public class PingResponse
    {
        public string Status { get; set; } = "OK";
        public DateTime Timestamp { get; set; }
    }
}

