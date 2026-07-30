namespace AuPairGo.API.DTOs
{
    public class CreateChildProfileDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Allergies { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string SpecialInstructions { get; set; } = string.Empty;
    }
}