namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class DroneConnectionAnalysisResponse
    {
        public string TargetDroneId { get; set; }
        public bool CanConnect { get; set; }
        public string? EntryRelayDroneId { get; set; }
        public IReadOnlyCollection<string> Path { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<double> PathWeights { get; set; } = Array.Empty<double>();
        public double MinimumLinkWeight { get; set; }
        public int HopCount => Math.Max(Path.Count - 1, 0);
    }
}

