namespace DevOpsProject.Shared.Models.Entities;

public class EwZone
{
    public Guid Id { get; set; }              // InterferenceModel.Id
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusKm { get; set; }

    public bool IsActive { get; set; }
    public DateTime ActiveFromUtc { get; set; }
    public DateTime? ActiveToUtc { get; set; }
}