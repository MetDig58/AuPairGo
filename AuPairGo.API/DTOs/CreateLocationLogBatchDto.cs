using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class CreateLocationLogBatchDto
    {
        [Required(ErrorMessage = "DutyTaskId is required.")]
        public int DutyTaskId { get; set; }

        [Required(ErrorMessage = "Locations are required.")]
        [MinLength(1, ErrorMessage = "At least one location is required.")]
        public List<OfflineLocationPointDto> Locations { get; set; } = new();
    }

    public class OfflineLocationPointDto
    {
        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0,
            ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0,
            ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "CapturedAt is required.")]
        public DateTimeOffset CapturedAt { get; set; }
    }
}