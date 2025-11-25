namespace DevOpsProject.Shared.Models.Entities;

public class HiveRepositionSuggestion
{
    public long Id { get; set; }

    public string SourceHiveId { get; set; }
    public string OtherHiveId  { get; set; }

    public float SourceLatitude  { get; set; }
    public float SourceLongitude { get; set; }

    public float OtherLatitude  { get; set; }
    public float OtherLongitude { get; set; }

    public double DistanceKm { get; set; }

    public DateTime SuggestedAtUtc { get; set; }
    public bool IsConsumed { get; set; }
}