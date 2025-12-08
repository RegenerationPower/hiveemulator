#nullable enable
using System.Collections.Generic;

namespace DevOpsProject.Shared.Models.DTO.Topology
{
    public class DroneConnectionAnalysisResponse
    {
        public string TargetDroneId { get; set; } = string.Empty;
        public bool CanConnect { get; set; }
        public string? EntryRelayDroneId { get; set; }
        public IReadOnlyCollection<string> Path { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<double> PathWeights { get; set; } = Array.Empty<double>();
        public double MinimumLinkWeight { get; set; }
        public int HopCount => Math.Max(Path.Count - 1, 0);
    }

    public class SwarmConnectivityResponse
    {
        public string HiveId { get; set; } = string.Empty;
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

    public class MeshCommandResponse
    {
        public bool Success { get; set; }
        public string TargetDroneId { get; set; } = string.Empty;
        public IReadOnlyCollection<string> RoutePath { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<double> RouteWeights { get; set; } = Array.Empty<double>();
        public double MinimumLinkWeight { get; set; }
        public int HopCount { get; set; }
        public int RelaysUsed { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

