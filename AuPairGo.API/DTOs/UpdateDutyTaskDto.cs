using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class UpdateDutyTaskDto
    {
        [Required]
        public int ChildProfileId { get; set; }

        [Range(1, int.MaxValue)]
        public int PickupAddressId { get; set; }

        [Range(1, int.MaxValue)]
        public int DropOffAddressId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledStartTime { get; set; }

        public DateTime? ScheduledEndTime { get; set; }
    }
}