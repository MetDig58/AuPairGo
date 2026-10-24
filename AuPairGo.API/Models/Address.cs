using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }

        public int? ParentProfileId { get; set; }

        [ForeignKey(nameof(ParentProfileId))]
        public ParentProfile? ParentProfile { get; set; }

        [Required]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        public string Suburb { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double latitude { get; set; }

        [Range(-180, 180)]
        public double longitude { get; set; }
    }
}