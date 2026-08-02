using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    [Table("location_logs")]
    public class LocationLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Required]
        [Column("au_pair_id")]
        public int AuPairId { get; set; }

        [Required]
        [Column("parent_id")]
        public int ParentId { get; set; }

        [Column("child_id")]
        public int? ChildId { get; set; }

        [Required]
        [Column("latitude")]
        public double Latitude { get; set; }

        [Required]
        [Column("longitude")]
        public double Longitude { get; set; }

        [Required]
        [Column("timestamp")]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}