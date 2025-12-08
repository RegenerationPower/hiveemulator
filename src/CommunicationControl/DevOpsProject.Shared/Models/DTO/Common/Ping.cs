#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.Common
{
    public class PingRequest
    {
        public DateTime Timestamp { get; set; }

        [Required]
        public string HiveID { get; set; } = string.Empty;
    }

    public class PingResponse
    {
        public string Status { get; set; } = "OK";
        public DateTime Timestamp { get; set; }
    }
}

