using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class AddChildDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Allergies { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    public class UpdateChildDto
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

    public class ChildResponseDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? Allergies { get; set; }
        public string? SchoolName { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}