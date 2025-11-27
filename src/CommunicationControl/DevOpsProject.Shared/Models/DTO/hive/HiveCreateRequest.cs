using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class HiveCreateRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}

