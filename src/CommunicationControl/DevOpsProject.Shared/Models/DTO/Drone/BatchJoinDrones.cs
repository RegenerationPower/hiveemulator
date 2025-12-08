#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.Drone
{
    public class BatchJoinDronesRequest
    {
        [Required]
        public List<string> DroneIds { get; set; } = new();
    }

    public class BatchJoinDronesResponse
    {
        public string HiveId { get; set; } = string.Empty;
        public int TotalRequested { get; set; }
        public int Joined { get; set; }
        public int AlreadyInHive { get; set; }
        public int Failed { get; set; }
        public IReadOnlyCollection<string> JoinedDroneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> AlreadyInHiveDroneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<BatchJoinError> Errors { get; set; } = Array.Empty<BatchJoinError>();
    }

    public class BatchJoinError
    {
        public string DroneId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

