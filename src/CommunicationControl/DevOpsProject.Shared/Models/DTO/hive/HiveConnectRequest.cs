using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class HiveConnectRequest
    {
        [Required]
        public string HiveSchema { get; set; } = string.Empty;

        [Required]
        public string HiveIP { get; set; } = string.Empty;

        [Required]
        public int HivePort { get; set; }
        
        [Required]        
        public string HiveID { get; set; } = string.Empty;
    }

    public class HiveConnectResponse
    {
        public bool ConnectResult { get; set; }
        public HiveOperationalArea OperationalArea { get; set; } = new();
        public List<InterferenceModel> Interferences { get; set; } = new();
    }
}
