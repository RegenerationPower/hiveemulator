using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class StopHivesMovementRequest
    {
        [Required]
        public List<string> Hives { get; set; }
    }
}
