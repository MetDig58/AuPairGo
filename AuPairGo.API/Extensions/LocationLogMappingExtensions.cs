using AuPairGo.API.DTOs;
using AuPairGo.API.Models;

namespace AuPairGo.API.Extensions
{
    public static class LocationLogMappingExtensions
    {
        public static LocationLogResponseDto ToResponseDto(this LocationLog log)
        {
            return new LocationLogResponseDto
            {
                Id = log.Id,
                DutyTaskId = log.DutyTaskId,
                ChildId = log.ChildId,
                Latitude = log.Latitude,
                Longitude = log.Longitude,
                TimeStamp = log.TimeStamp
            };
        }
    }
}