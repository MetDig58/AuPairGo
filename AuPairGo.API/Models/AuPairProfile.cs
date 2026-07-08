using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public class AuPairProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string Bio { get; set; } = string.Empty;

        [Required]
        public decimal HourlyRate { get; set; }

        [Required]
        public bool HasDriversLicense { get; set; }

        [Required]
        public string Experience { get; set; } = string.Empty;
    }
}