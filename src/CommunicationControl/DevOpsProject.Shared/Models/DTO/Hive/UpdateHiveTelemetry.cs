using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.Hive
{
    public class UpdateHiveTelemetryRequest
    {
        public Location? Location { get; set; }
        public float? Height { get; set; }
        public float? Speed { get; set; }
        public bool? IsMoving { get; set; }
    }

    public class UpdateHiveTelemetryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public HiveTelemetryModel? Telemetry { get; set; }
    }
}

