using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool TryGetAuthenticatedUserId(out int userId)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userIdValue, out userId);
        }

        private static bool IsDutyParticipant(
            DutyTask dutyTask,
            int authenticatedUserId)
        {
            return dutyTask.ParentProfile?.UserId ==
                       authenticatedUserId
                   || dutyTask.AuPairProfile?.UserId ==
                       authenticatedUserId;
        }

        [HttpPost("logs")]
        public async Task<IActionResult> LogLocation(
            [FromBody] CreateLocationLogDto dto)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(duty =>
                    duty.Id == dto.DutyTaskId);

            if (dutyTask == null)
            {
                return NotFound(
                    $"Duty task with ID {dto.DutyTaskId} was not found.");
            }

            if (!IsDutyParticipant(
                    dutyTask,
                    authenticatedUserId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned parent or au pair can log location for this duty.");
            }

            var locationLog = new LocationLog
            {
                DutyTaskId = dutyTask.Id,
                ChildId = dutyTask.ChildProfileId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TimeStamp = DateTime.UtcNow
            };

            _context.LocationLogs.Add(locationLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetLocationLogsForDuty),
                new { dutyTaskId = locationLog.DutyTaskId },
                locationLog.ToResponseDto());
        }

        [HttpGet("duties/{dutyTaskId}")]
        public async Task<IActionResult> GetLocationLogsForDuty(
            int dutyTaskId)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(duty =>
                    duty.Id == dutyTaskId);

            if (dutyTask == null)
            {
                return NotFound(
                    $"Duty task with ID {dutyTaskId} was not found.");
            }

            if (!IsDutyParticipant(
                    dutyTask,
                    authenticatedUserId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned parent or au pair can view location for this duty.");
            }

            var logs = await _context.LocationLogs
                .AsNoTracking()
                .Where(location =>
                    location.DutyTaskId == dutyTaskId)
                .OrderByDescending(location =>
                    location.TimeStamp)
                .ToListAsync();

            return Ok(logs
                .Select(location => location.ToResponseDto())
                .ToList());
        }
    }
}