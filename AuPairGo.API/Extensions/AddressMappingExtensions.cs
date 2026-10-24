using AuPairGo.API.DTOs;
using AuPairGo.API.Models;

namespace AuPairGo.API.Extensions
{
    public static class AddressMappingExtensions
    {
        public static AddressResponseDto ToResponseDto(this Address address)
        {
            return new AddressResponseDto
            {
                Id = address.Id,
                ParentProfileId = address.ParentProfileId,
                StreetAddress = address.StreetAddress,
                Suburb = address.Suburb,
                City = address.City,
                PostalCode = address.PostalCode,
                latitude = address.latitude,
                longitude = address.longitude
            };
        }
    }
}