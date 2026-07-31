using System.ComponentModel.DataAnnotations;
using AuPairGo.API.Models;

namespace AuPairGo.API.DTOs
{
    public class UpdateDutyStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        public DutyStatus Status { get; set; }
    }
}