#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DevOpsProject.Shared.Models.Commands.Drone
{
    public class RelayDroneCommandPayload
    {
        [Required]
        [JsonPropertyName("finalDestinationDroneId")]
        public string FinalDestinationDroneId { get; set; } = string.Empty;

        [JsonPropertyName("nextHopDroneId")]
        public string? NextHopDroneId { get; set; }

        [Required]
        [JsonPropertyName("finalCommand")]
        public DroneCommand FinalCommand { get; set; } = new();

        [Required]
        [JsonPropertyName("routePath")]
        public IReadOnlyCollection<string> RoutePath { get; set; } = Array.Empty<string>();
    }
}

