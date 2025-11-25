namespace DevOpsProject.Shared.Models.Entities;

public class EwZoneHistory
{
    public long Id { get; set; }

    public Guid ZoneId { get; set; }

    public double CenterLatitude  { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusKm        { get; set; }

    public bool IsActive { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}