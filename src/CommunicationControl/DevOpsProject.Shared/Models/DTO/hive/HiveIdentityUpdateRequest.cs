#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class HiveIdentityUpdateRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;
        public bool Reconnect { get; set; } = true;
    }
}

