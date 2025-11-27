using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class TopologyRebuildRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;

        [Required]
        public string TopologyType { get; set; } = string.Empty;

        public double DefaultWeight { get; set; } = 0.8;
    }

    public class TopologyRebuildResponse
    {
        public bool Success { get; set; }
        public string HiveId { get; set; } = string.Empty;
        public string TopologyType { get; set; } = string.Empty;
        public int ConnectionsCreated { get; set; }
        public int ConnectionsRemoved { get; set; }
        public IReadOnlyCollection<string>? EntryRelays { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

