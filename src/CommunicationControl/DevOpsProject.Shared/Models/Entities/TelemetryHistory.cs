using DevOpsProject.Shared.Enums;

namespace DevOpsProject.Shared.Models.Entities;

public class TelemetryHistory
{
    public long Id { get; set; }

    public string HiveId { get; set; }

    public float Latitude  { get; set; }
    public float Longitude { get; set; }
    public float Height    { get; set; }
    public float Speed     { get; set; }
    public HiveMindState State { get; set; }

    public bool IsInEwZone { get; set; }

    public DateTime TimestampUtc { get; set; }
}