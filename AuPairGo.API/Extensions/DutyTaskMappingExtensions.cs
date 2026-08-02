using AuPairGo.API.DTOs;
using AuPairGo.API.Models;

namespace AuPairGo.API.Extensions
{
    public static class DutyTaskMappingExtensions
    {
        public static DutyTaskResponseDto ToResponseDto(this DutyTask task)
        {
            return new DutyTaskResponseDto
            {
                Id = task.Id,
                ParentProfileId = task.ParentProfileId,
                AuPairProfileId = task.AuPairProfileId,
                Title = task.Title,
                Description = task.Description,
                ScheduledStartTime = task.ScheduledStartTime,
                ScheduledEndTime = task.ScheduledEndTime,
                Status = task.Status.ToString(),
                CreatedAt = task.CreatedAt
            };
        }
    }
}