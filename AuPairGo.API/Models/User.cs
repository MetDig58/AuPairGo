using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        public string CellNumber { get; set; } = string.Empty;
        public string? IdNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string OneSignalPlayerId { get; set; } = string.Empty;
    }
}