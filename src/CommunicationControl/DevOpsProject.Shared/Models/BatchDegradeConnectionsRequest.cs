using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class BatchDegradeConnectionsRequest
    {
        [Required]
        public List<DegradeConnectionRequest> Connections { get; set; } = new();
    }
}

