namespace AuPairGo.API.DTOs
{
    public class ChildProfileResponseDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? Allergies { get; set; }
        public string? SchoolName { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}