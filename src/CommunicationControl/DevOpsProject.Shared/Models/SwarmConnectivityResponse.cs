namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class SwarmConnectivityResponse
    {
        public string HiveId { get; set; }

        public bool IsFullyConnected { get; set; }

        public int ConnectedComponents { get; set; }

        public IReadOnlyCollection<IReadOnlyCollection<string>> IsolatedGroups { get; set; } = Array.Empty<IReadOnlyCollection<string>>();

        public int TotalDrones { get; set; }

        public int LargestComponentSize { get; set; }

        public IReadOnlyCollection<ComponentInfo> Components { get; set; } = Array.Empty<ComponentInfo>();

        public int TotalConnections { get; set; }

        public double AverageConnectionWeight { get; set; }

        public double MinimumConnectionWeight { get; set; }

        public double MaximumConnectionWeight { get; set; }

        public IReadOnlyDictionary<string, IReadOnlyCollection<ConnectionInfo>> ConnectionGraph { get; set; } = new Dictionary<string, IReadOnlyCollection<ConnectionInfo>>();
    }

    public class ComponentInfo
    {
        public int ComponentId { get; set; }

        public int DroneCount { get; set; }

        public IReadOnlyCollection<string> DroneIds { get; set; } = Array.Empty<string>();

        public int ConnectionCount { get; set; }
    }

    public class ConnectionInfo
    {
        public string TargetDroneId { get; set; } = string.Empty;

        public double Weight { get; set; }
    }
}

