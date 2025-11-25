using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.CommunicationControl.API.DTO.Client.Request
{
    public class StopHivesMovementRequest
    {
        [Required]
        public List<string> Hives { get; set; }
    }
}
