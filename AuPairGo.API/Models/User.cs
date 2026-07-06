namespace AuPairGo.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CellNumber { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // 'Parent', 'AuPair', 'Child'
        public string OneSignalPlayerId { get; set; } = string.Empty;
    }
}