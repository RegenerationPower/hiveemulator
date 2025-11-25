using DevOpsProject.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace DevOpsProject.CommunicationControl.API.DTO.Client.Request
{
    public class MoveHivesRequest
    {
        [Required]
        public List<string> Hives { get;set;}
        public Location Destination { get; set; }
    }
}
