using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
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

