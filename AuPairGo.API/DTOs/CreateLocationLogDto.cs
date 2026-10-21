using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class CreateLocationLogDto
    {
        [Required(ErrorMessage = "DutyTaskId is required.")]
        public int DutyTaskId { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0,
            ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0,
            ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }
    }
}