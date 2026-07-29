using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class CreateParentProfileDto
    {
        [Required]
        public int UserId { get; set; }

        public string Bio { get; set; } = string.Empty;

        [Required]
        [Range(1, 20, ErrorMessage = "Number of children must be at least 1.")]
        public int NumberOfChildren { get; set; }

        [Required(ErrorMessage = "Child age groups are required.")]
        public string ChildAgeGroups { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        public string? Suburb { get; set; }
    }
}