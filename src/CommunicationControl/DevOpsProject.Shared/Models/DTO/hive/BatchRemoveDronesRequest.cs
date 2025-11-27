using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class BatchRemoveDronesRequest
    {
        [Required]
        public IReadOnlyCollection<string> DroneIds { get; set; } = new List<string>();
    }

    public class BatchRemoveDronesResponse
    {
        public string HiveId { get; set; } = string.Empty;
        public int TotalRequested { get; set; }
        public int Removed { get; set; }
        public int NotInHive { get; set; }
        public int Failed { get; set; }
        public IReadOnlyCollection<string> RemovedDroneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> NotInHiveDroneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<BatchRemoveError> Errors { get; set; } = Array.Empty<BatchRemoveError>();
    }

    public class BatchRemoveError
    {
        public string DroneId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

