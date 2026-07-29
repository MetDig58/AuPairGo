using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class UpdateChildProfileDto
    {
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "School name is required.")]
        public string SchoolName { get; set; } = string.Empty;

        // Optional fields
        public string? Allergies { get; set; } = string.Empty;
        public string? SpecialInstructions { get; set; } = string.Empty;
    }

}