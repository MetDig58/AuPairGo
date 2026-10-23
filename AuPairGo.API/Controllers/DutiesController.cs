using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DutiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DutiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool TryGetAuthenticatedUserId(out int userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userIdValue, out userId);
        }

        // POST: api/duties
        [HttpPost]
        public async Task<IActionResult> CreateDutyTask([FromBody] CreateDutyTaskDto dto)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (dto.ScheduledEndTime.HasValue && dto.ScheduledEndTime <= dto.ScheduledStartTime)
            {
                return BadRequest("ScheduledEndTime must be after ScheduledStartTime.");
            }

            var parent = await _context.ParentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.ParentProfileId && p.UserId == authenticatedUserId);
            if (parent == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only create duties for your own parent profile.");
            }

            var addressesExist = await _context.Addresses.AsNoTracking().Where(address => address.Id == dto.PickupAddressId || address.Id == dto.DropOffAddressId).Select(address => address.Id).ToListAsync();

            if (!addressesExist.Contains(dto.PickupAddressId))
            {
                return NotFound(
                    $"Pickup address with ID {dto.PickupAddressId} was not found.");
            }

            if (!addressesExist.Contains(dto.DropOffAddressId))
            {
                return NotFound(
                    $"Drop-off address with ID {dto.DropOffAddressId} was not found.");
            }

            var auPairExists = await _context.AuPairProfiles.AnyAsync(a => a.Id == dto.AuPairProfileId);
            if (!auPairExists)
            {
                return NotFound($"Au Pair profile with ID {dto.AuPairProfileId} was not found.");
            }

            var childBelongsToParent = await _context.ChildProfiles
            .Join(
            _context.ParentProfiles,
            child => child.UserId,
            parent => parent.UserId,
            (child, parent) => new
            {
                ChildId = child.Id,
                ParentProfileId = parent.Id
            })
            .AnyAsync(x =>
                x.ChildId == dto.ChildProfileId &&
                x.ParentProfileId == dto.ParentProfileId);

            if (!childBelongsToParent)
            {
                return BadRequest(
                    $"Child profile with ID {dto.ChildProfileId} does not belong to parent profile ID {dto.ParentProfileId}.");
            }


            var dutyTask = new DutyTask
            {
                ParentProfileId = dto.ParentProfileId,
                AuPairProfileId = dto.AuPairProfileId,
                ChildProfileId = dto.ChildProfileId,
                PickupAddressId = dto.PickupAddressId,
                DropOffAddressId = dto.DropOffAddressId,
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDutyById(int id)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .AsNoTracking()
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .FirstOrDefaultAsync(duty => duty.Id == id);

            if (dutyTask == null)
            {
                return NotFound($"Duty task with ID {id} was not found.");
            }

            var isParent =
                dutyTask.ParentProfile?.UserId == authenticatedUserId;

            var isAuPair =
                dutyTask.AuPairProfile?.UserId == authenticatedUserId;

            if (!isParent && !isAuPair)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only view duties connected to your account.");
            }

            return Ok(dutyTask.ToResponseDto());
        }

        [HttpGet("parent/{parentProfileId}")]
        public async Task<IActionResult> GetDutiesForParent(
    int parentProfileId)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var parent = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(parent =>
                    parent.Id == parentProfileId);

            if (parent == null)
            {
                return NotFound(
                    $"Parent profile with ID {parentProfileId} was not found.");
            }

            if (parent.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only view duties for your own parent profile.");
            }

            var duties = await _context.DutyTasks
                .AsNoTracking()
                .Where(duty =>
                    duty.ParentProfileId == parentProfileId)
                .OrderBy(duty => duty.ScheduledStartTime)
                .ToListAsync();

            return Ok(duties
                .Select(duty => duty.ToResponseDto())
                .ToList());
        }

        // GET: api/duties/aupair/{auPairId}
        [HttpGet("aupair/{auPairId}")]
        public async Task<IActionResult> GetDutiesForAuPair(int auPairId)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            var auPairExists = await _context.AuPairProfiles
                .AsNoTracking()
                .AnyAsync(auPair => auPair.Id == auPairId);

            if (!auPairExists)
            {
                return NotFound($"Au pair profile with ID {auPairId} was not found.");
            }

            var canViewDuties = await _context.DutyTasks
                .AsNoTracking()
                .AnyAsync(duty =>
                    duty.AuPairProfileId == auPairId &&
                    (
                        duty.AuPairProfile != null &&
                        duty.AuPairProfile.UserId == authenticatedUserId
                        ||
                        duty.ParentProfile != null &&
                        duty.ParentProfile.UserId == authenticatedUserId
                    ));

            if (!canViewDuties)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only view duties assigned to you or your parent account.");
            }

            var duties = await _context.DutyTasks
                .AsNoTracking()
                .Where(duty =>
                    duty.AuPairProfileId == auPairId &&
                    (
                        duty.AuPairProfile != null &&
                        duty.AuPairProfile.UserId == authenticatedUserId
                        ||
                        duty.ParentProfile != null &&
                        duty.ParentProfile.UserId == authenticatedUserId
                    ))
                .OrderBy(duty => duty.ScheduledStartTime)
                .ToListAsync();

            return Ok(duties.Select(duty => duty.ToResponseDto()).ToList());
        }

        // PATCH: api/duties/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateDutyStatus(int id, [FromBody] UpdateDutyStatusDto dto)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .FirstOrDefaultAsync(duty => duty.Id == id);

            if (dutyTask == null)
            {
                return NotFound($"Duty Task with the id: {id} was not found.");
            }

            var isParent = dutyTask.ParentProfile?.UserId == authenticatedUserId;
            var isAuPair = dutyTask.AuPairProfile?.UserId == authenticatedUserId;

            if (!isParent && !isAuPair)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only update duties connected to your account.");
            }

            dutyTask.Status = dto.Status;

            await _context.SaveChangesAsync();

            return Ok(dutyTask.ToResponseDto());
        }
    }
}