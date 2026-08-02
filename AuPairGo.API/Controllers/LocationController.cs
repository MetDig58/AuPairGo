using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]

    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Post: api/location/logs
        [HttpPost("logs")]
        public async Task<IActionResult> LogLocation([FromBody] CreateLocationLogDto dto)
        {
            var LocationLog = new LocationLog
            {
                ScheduleId = dto.ScheduleId,
                AuPairId = dto.AuPairId,
                ParentId = dto.ParentId,
                ChildId = dto.ChildId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TimeStamp = DateTime.UtcNow
            };

            _context.LocationLogs.Add(LocationLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetLocationLogsForDuty),
                new { DutyTaskId = LocationLog.ScheduleId },
                LocationLog.ToResponseDto()
            );
        }

        [HttpGet("duties/{DutyTaskId}")]
        public async Task<IActionResult> GetLocationLogsForDuty(int DutyTaskId)
        {
            var logs = await _context.LocationLogs
                .AsNoTracking()
                .Where(Location => Location.ScheduleId == DutyTaskId)
                .OrderByDescending(Location => Location.TimeStamp)
                .Select(Location => Location.ToResponseDto())
                .ToListAsync();

            return Ok(logs);
        }

    }
}