using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models.DTO.Bms;

public class HiveStatusDto
{
    public string HiveId { get; set; }
    public float Latitude  { get; set; }
    public float Longitude { get; set; }
    public float Height    { get; set; }
    public float Speed     { get; set; }
    public HiveMindState State { get; set; }
    public bool IsInEwZone { get; set; }
    public DateTime LastTelemetryTimestampUtc { get; set; }
}