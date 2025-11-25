using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class HiveConnectRequest
    {
        [Required]
        public string HiveSchema { get; set; }

        [Required]
        public string HiveIP { get; set; }

        [Required]
        public int HivePort { get; set; }
        
        [Required]        
        public string HiveID { get; set; }
    }
}
