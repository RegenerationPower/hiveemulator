namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class DroneConnectionAnalysisResponse
    {
        public Guid TargetDroneId { get; set; }
        public bool CanConnect { get; set; }
        public Guid? EntryRelayDroneId { get; set; }
        public IReadOnlyCollection<Guid> Path { get; set; } = Array.Empty<Guid>();
        public double MinimumLinkWeight { get; set; }
        public int HopCount => Math.Max(Path.Count - 1, 0);
    }
}

