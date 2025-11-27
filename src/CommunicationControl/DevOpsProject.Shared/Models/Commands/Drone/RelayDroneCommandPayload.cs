#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.Commands.Drone
{
    public class RelayDroneCommandPayload
    {
        [Required]
        public string FinalDestinationDroneId { get; set; } = string.Empty;

        public string? NextHopDroneId { get; set; }

        [Required]
        public DroneCommand FinalCommand { get; set; } = new();

        [Required]
        public IReadOnlyCollection<string> RoutePath { get; set; } = Array.Empty<string>();
    }
}

