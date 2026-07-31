using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public class DutyTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ParentProfileId { get; set; }

        [ForeignKey("ParentProfileId")]
        public ParentProfile? ParentProfile { get; set; }

        [Required]
        public int AuPairProfileId { get; set; }

        [ForeignKey("AuPairProfileId")]
        public AuPairProfile? AuPairProfile { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledStartTime { get; set; }

        [Required]
        public DateTime? ScheduledEndTime { get; set; }

        [Required]
        public DutyStatus Status { get; set; } = DutyStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}