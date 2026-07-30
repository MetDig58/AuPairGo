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
        public string? Suburb { get; set; }

        [Required]
        public bool HasDriversLicense { get; set; }

        [Required]
        public bool HasCar { get; set; }

        [Required]
        public bool HasFirstAid { get; set; }

        [Required]
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }

        [Required]
        public string Availability { get; set; } = string.Empty;
    }
}