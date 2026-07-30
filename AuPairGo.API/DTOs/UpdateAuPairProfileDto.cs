using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class UpdateAuPairProfileDto
    {
        [Required(ErrorMessage = "Bio is required.")]
        public string Bio { get; set; } = string.Empty;

        public string Suburb { get; set; } = string.Empty;

        public bool HasDriversLicense { get; set; }

        public bool HasCar { get; set; }

        public bool HasFirstAid { get; set; }

        [Required(ErrorMessage = "Years of experience is required.")]
        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50.")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Availability is required.")]
        public string Availability { get; set; } = string.Empty;
    }

}