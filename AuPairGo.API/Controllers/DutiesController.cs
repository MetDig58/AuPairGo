using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
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

        // POST: api/duties
        [HttpPost]
        public async Task<IActionResult> CreateDutyTask([FromBody] CreateDutyTaskDto dto)
        {
            if (dto.ScheduledEndTime.HasValue && dto.ScheduledEndTime <= dto.ScheduledStartTime)
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
                ScheduledEndTime = dto.ScheduledEndTime,
                Status = DutyStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.DutyTasks.Add(dutyTask);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDutiesForAuPair),
                new { auPairId = dutyTask.AuPairProfileId },
                dutyTask.ToResponseDto()
            );
        }

        // GET: api/duties/aupair/{auPairId}
        [HttpGet("aupair/{auPairId}")]
        public async Task<IActionResult> GetDutiesForAuPair(int auPairId)
        {
            var duties = await _context.DutyTasks
                .AsNoTracking()
                .Where(d => d.AuPairProfileId == auPairId)
                .OrderBy(d => d.ScheduledStartTime)
                .Select(d => d.ToResponseDto())
                .ToListAsync();

            return Ok(duties);
        }

        // PATCH: api/duties/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateDutyStatus(int id, [FromBody] UpdateDutyStatusDto dto)
        {
            var dutyTask = await _context.DutyTasks.FindAsync(id);

            if (dutyTask == null)
            {
                return NotFound($"Duty Task with the id: {id} was not found.");
            }

            dutyTask.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(dutyTask.ToResponseDto());
        }
    }
}