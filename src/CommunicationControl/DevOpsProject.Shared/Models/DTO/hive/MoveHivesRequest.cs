using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class MoveHivesRequest
    {
        [Required]
        public List<string> Hives { get;set;}
        public Location Destination { get; set; }
    }
}
