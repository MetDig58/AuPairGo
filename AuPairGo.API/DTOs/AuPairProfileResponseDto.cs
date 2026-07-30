namespace AuPairGo.API.DTOs
{
    public class AuPairProfileResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // Joined from User
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CellNumber { get; set; } = string.Empty;

        // Au Pair specific
        public string Bio { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string Availability { get; set; } = string.Empty;
        public bool HasFirstAid { get; set; }
        public bool HasDriversLicense { get; set; }
        public bool HasCar { get; set; }
        public string? Suburb { get; set; }
    }
}