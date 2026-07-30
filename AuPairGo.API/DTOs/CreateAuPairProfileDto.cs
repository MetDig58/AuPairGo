using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class CreateAuPairProfileDto
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Bio is required.")]
        public string Bio { get; set; } = string.Empty;

        [Required(ErrorMessage = "Years of experience is required.")]
        [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years.")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Availability is required.")]
        public string Availability { get; set; } = string.Empty;

        public bool HasFirstAid { get; set; } = false;

        public bool HasDriversLicense { get; set; } = false;

        public bool HasCar { get; set; } = false;

        public string? Suburb { get; set; }
    }
}