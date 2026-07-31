using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DutiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DutiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Post: api/duties
        [HttpPost]
        public async Task<IActionResult> CreateDutyTask([FromBody] CreateDutyTaskDto dto)
        {
            if (dto.ScheduledEndTime <= dto.ScheduledStartTime)
            {
                return BadRequest("ScheduledEndTime must be after ScheduledStartTime.");
            }

            var parentExists = await _context.ParentProfiles.AnyAsync(p => p.Id == dto.ParentProfileId);
            if (!parentExists)
            {
                return NotFound($"Parent profile with ID {dto.ParentProfileId} was not found.");
            }

            var auPairExists = await _context.AuPairProfiles.AnyAsync(a => a.Id == dto.AuPairProfileId);
            if (!auPairExists)
            {
                return NotFound($"Au Pair profile with ID {dto.AuPairProfileId} was not found.");
            }

            var dutyTask = new DutyTask
            {
                ParentProfileId = dto.ParentProfileId,
                AuPairProfileId = dto.AuPairProfileId,
                Title = dto.Title,
                Description = dto.Description,
                ScheduledStartTime = dto.ScheduledStartTime,
                ScheduledEndTime = dto.ScheduledEndTime, // Can be null
                Status = DutyStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.DutyTasks.Add(dutyTask);
            await _context.SaveChangesAsync();

            var response = MapToResponseDto(dutyTask);

            return CreatedAtAction(nameof(GetDutiesForAuPair), new { auPairId = dutyTask.AuPairProfileId }, response);
        }

        //Get: api/duties/aupair/{AuPairId}
        [HttpGet("aupair/{AuPairId}")]
        public async Task<IActionResult> GetDutiesForAuPair(int AuPairId)
        {
            var Duties = await _context.DutyTasks
            .AsNoTracking()
            .Where(Duties => Duties.AuPairProfileId == AuPairId)
            .OrderBy(Duties => Duties.ScheduledStartTime)
            .Select(Duties => MapToResponseDto(Duties))
            .ToListAsync();

            return Ok(Duties);
        }

        //Patch: api/duties/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateDutyStatus(int id, [FromBody] UpdateDutyStatusDto dto)
        {
            var DutyTask = await _context.DutyTasks.FindAsync(id);

            if (DutyTask == null)
            {
                return NotFound($"Duty Task with the id: {id} was not found.");
            }

            DutyTask.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(MapToResponseDto(DutyTask));
        }

        private static DutyTaskResponseDto MapToResponseDto(DutyTask DutyTask)
        {
            return new DutyTaskResponseDto
            {
                Id = DutyTask.Id,
                ParentProfileId = DutyTask.ParentProfileId,
                AuPairProfileId = DutyTask.AuPairProfileId,
                Title = DutyTask.Title,
                Description = DutyTask.Description,
                ScheduledStartTime = DutyTask.ScheduledStartTime,
                ScheduledEndTime = DutyTask.ScheduledEndTime,
                Status = DutyTask.Status.ToString(),
                CreatedAt = DutyTask.CreatedAt
            };
        }
    }
}