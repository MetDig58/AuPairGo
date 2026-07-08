using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public class ChildProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string Allergies { get; set; } = string.Empty;

        [Required]
        public string SchoolName { get; set; } = string.Empty;

        [Required]
        public string SpecialInstructions { get; set; } = string.Empty;
    }
}