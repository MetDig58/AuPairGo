namespace AuPairGo.API.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string StreetAddress { get; set; } = string.Empty;
        public string Suburb { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double latitude { get; set; }
        public double longitude { get; set; }
    }
}