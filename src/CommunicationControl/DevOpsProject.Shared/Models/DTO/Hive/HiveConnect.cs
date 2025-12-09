using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DevOpsProject.Shared.Models
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
        public HiveOperationalArea OperationalArea { get; set; }
        public List<InterferenceModel> Interferences { get; set; }
    }
}

