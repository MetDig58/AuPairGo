using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class CreateDutyTaskDto
    {
        [Required(ErrorMessage = "ParentProfileId is required.")]
        public int ParentProfileId { get; set; }

        [Required(ErrorMessage = "AuPairProfileId is required.")]
        public int AuPairProfileId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scheduled start time is required.")]
        public DateTime ScheduledStartTime { get; set; }

        public DateTime ScheduledEndTime { get; set; }
    }
}