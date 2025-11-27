namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class MeshCommandResponse
    {
        public bool Success { get; set; }

        public string TargetDroneId { get; set; }

        public IReadOnlyCollection<string> RoutePath { get; set; } = Array.Empty<string>();

        public IReadOnlyCollection<double> RouteWeights { get; set; } = Array.Empty<double>();

        public double MinimumLinkWeight { get; set; }

        public int HopCount { get; set; }

        public int RelaysUsed { get; set; }

        public string? ErrorMessage { get; set; }
    }
}

