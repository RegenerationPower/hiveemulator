using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class ConnectToHiveMindRequest
    {
        [Required]
        public string HiveId { get; set; } = string.Empty;

        [JsonPropertyName("entryRelayIds")]
        public List<string>? EntryRelayIds { get; set; }

        [JsonPropertyName("hubDroneIds")]
        public List<string>? LegacyHubDroneIds
        {
            get => EntryRelayIds;
            set => EntryRelayIds = value;
        }
    }
}

