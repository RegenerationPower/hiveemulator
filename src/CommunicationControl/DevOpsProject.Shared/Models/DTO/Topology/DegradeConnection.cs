#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.Topology
{
    public class DegradeConnectionRequest
    {
        [Required]
        public string FromDroneId { get; set; } = string.Empty;

        [Required]
        public string ToDroneId { get; set; } = string.Empty;

        [Range(0.0, 1.0)]
        public double NewWeight { get; set; }
    }

    public class DegradeConnectionResponse
    {
        public bool Success { get; set; }
        public string FromDroneId { get; set; } = string.Empty;
        public string ToDroneId { get; set; } = string.Empty;
        public double? PreviousWeight { get; set; }
        public double NewWeight { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BatchDegradeConnectionsRequest
    {
        [Required]
        public List<DegradeConnectionRequest> Connections { get; set; } = new();
    }

    public class BatchDegradeConnectionsResponse
    {
        public int TotalRequested { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public IReadOnlyCollection<DegradeConnectionResponse> Successful { get; set; } = Array.Empty<DegradeConnectionResponse>();
        public IReadOnlyCollection<DegradeConnectionResponse> FailedResults { get; set; } = Array.Empty<DegradeConnectionResponse>();
    }
}

