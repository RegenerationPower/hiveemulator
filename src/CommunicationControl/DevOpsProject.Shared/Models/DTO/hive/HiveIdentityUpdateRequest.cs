#nullable enable

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class HiveIdentityUpdateRequest
    {
        public string HiveId { get; set; } = string.Empty;
        public bool Reconnect { get; set; } = true;
    }
}

