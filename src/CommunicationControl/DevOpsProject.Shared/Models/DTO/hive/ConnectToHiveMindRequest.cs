using System.Text.Json.Serialization;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    /// <summary>
    /// Request to register HiveMind entry relays for a Hive
    /// </summary>
    public class ConnectToHiveMindRequest
    {
        /// <summary>
        /// The Hive ID
        /// </summary>
        public string HiveId { get; set; }

        /// <summary>
        /// Optional list of relay drone IDs that should act as entry points to HiveMind.
        /// If empty, HiveMind will auto-select available relay drones (up to two).
        /// </summary>
        [JsonPropertyName("entryRelayIds")]
        public List<string>? EntryRelayIds { get; set; }

        /// <summary>
        /// Legacy property name maintained for backward compatibility (hubDroneIds).
        /// Writing to this property populates EntryRelayIds.
        /// </summary>
        [JsonPropertyName("hubDroneIds")]
        public List<string>? LegacyHubDroneIds
        {
            get => EntryRelayIds;
            set => EntryRelayIds = value;
        }
    }
}

