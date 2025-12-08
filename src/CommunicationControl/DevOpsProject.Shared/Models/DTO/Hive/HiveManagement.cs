#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.Hive
{
    public class HiveCreateRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    public class HiveIdentityUpdateRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;
        public bool Reconnect { get; set; } = true;
    }
}

