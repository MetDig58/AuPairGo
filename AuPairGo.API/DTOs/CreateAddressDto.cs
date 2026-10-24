using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class CreateAddressDto
    {
        [Required]
        [StringLength(250)]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Suburb { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double latitude { get; set; }

        [Range(-180, 180)]
        public double longitude { get; set; }
    }
}