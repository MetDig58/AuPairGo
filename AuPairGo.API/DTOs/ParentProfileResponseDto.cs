namespace AuPairGo.API.DTOs
{
    public class ParentProfileResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // Pulled from User
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CellNumber { get; set; } = string.Empty;

        // Parent specific
        public string Bio { get; set; } = string.Empty;
        public int NumberOfChildren { get; set; }
        public string ChildAgeGroups { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Suburb { get; set; }
    }
}