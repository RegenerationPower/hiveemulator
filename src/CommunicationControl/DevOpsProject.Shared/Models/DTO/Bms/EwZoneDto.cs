namespace DevOpsProject.Shared.Models.DTO.Bms;

public class EwZoneDto
{
    public Guid Id { get; set; }
    public double CenterLatitude  { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusKm        { get; set; }
    public bool IsActive          { get; set; }
    public DateTime ActiveFromUtc { get; set; }
    public DateTime? ActiveToUtc  { get; set; }
}