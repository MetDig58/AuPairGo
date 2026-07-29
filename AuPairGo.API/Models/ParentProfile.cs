using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public class ParentProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
        public string Bio { get; set; } = string.Empty;

        [Required]
        public int NumberOfChildren { get; set; }

        [Required]
        public string ChildAgeGroups { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? Suburb { get; set; }
    }
}