using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AuPairGo.API.Services;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DutiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOneSignalService _oneSignalService;
        private readonly ILogger<DutiesController> _logger;

        public DutiesController(ApplicationDbContext context, IOneSignalService oneSignalService, ILogger<DutiesController> logger)
        {
            _context = context;
            _oneSignalService = oneSignalService;
            _logger = logger;
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

            var addressesBelongToParent = await _context.Addresses
                .AsNoTracking()
                .Where(address =>
                 address.ParentProfileId == parent.Id &&
                (address.Id == dto.PickupAddressId ||
                 address.Id == dto.DropOffAddressId))
                .Select(address => address.Id)
                .ToListAsync();

            if (!addressesBelongToParent.Contains(dto.PickupAddressId))
            {
                return BadRequest(
                    "The pickup address does not belong to this parent.");
            }

            if (!addressesBelongToParent.Contains(dto.DropOffAddressId))
            {
                return BadRequest(
                    "The drop-off address does not belong to this parent.");
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

        // PUT: api/duties/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDutyTask(int id, [FromBody] UpdateDutyTaskDto dto)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            if (dto.ScheduledEndTime.HasValue &&
                dto.ScheduledEndTime <= dto.ScheduledStartTime)
            {
                return BadRequest(
                    "ScheduledEndTime must be after ScheduledStartTime.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .FirstOrDefaultAsync(duty => duty.Id == id);

            if (dutyTask == null)
            {
                return NotFound($"Duty task with ID {id} was not found.");
            }

            if (dutyTask.ParentProfile?.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the parent who created the duty can update it.");
            }

            var addressesBelongToParent = await _context.Addresses
                .AsNoTracking()
                .Where(address =>
                address.ParentProfileId == dutyTask.ParentProfileId &&
                (address.Id == dto.PickupAddressId || address.Id == dto.DropOffAddressId))
                .Select(address => address.Id)
                .ToListAsync();

            if (!addressesBelongToParent.Contains(dto.PickupAddressId))
            {
                return BadRequest(
                    "The pickup address does not belong to this parent.");
            }

            if (!addressesBelongToParent.Contains(dto.DropOffAddressId))
            {
                return BadRequest(
                    "The drop-off address does not belong to this parent.");
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
                .AnyAsync(link =>
                    link.ChildId == dto.ChildProfileId &&
                    link.ParentProfileId == dutyTask.ParentProfileId);

            if (!childBelongsToParent)
            {
                return BadRequest(
                    $"Child profile with ID {dto.ChildProfileId} does not belong to this parent.");
            }

            dutyTask.ChildProfileId = dto.ChildProfileId;
            dutyTask.PickupAddressId = dto.PickupAddressId;
            dutyTask.DropOffAddressId = dto.DropOffAddressId;
            dutyTask.Title = dto.Title;
            dutyTask.Description = dto.Description;
            dutyTask.ScheduledStartTime = dto.ScheduledStartTime;
            dutyTask.ScheduledEndTime = dto.ScheduledEndTime;

            await _context.SaveChangesAsync();

            return Ok(dutyTask.ToResponseDto());
        }

        // DELETE: api/duties/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDutyTask(int id)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .FirstOrDefaultAsync(duty => duty.Id == id);

            if (dutyTask == null)
            {
                return NotFound($"Duty task with ID {id} was not found.");
            }

            if (dutyTask.ParentProfile?.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the parent who created the duty can cancel it.");
            }

            dutyTask.Status = DutyStatus.Cancelled;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/duties/{id}/check-in
        [HttpPost("{id}/check-in")]
        public async Task<IActionResult> CheckInToDuty(int id)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .FirstOrDefaultAsync(duty => duty.Id == id);

            if (dutyTask == null)
            {
                return NotFound($"Duty task with ID {id} was not found.");
            }

            if (dutyTask.AuPairProfile?.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned au pair can check in to this duty.");
            }

            if (dutyTask.Status == DutyStatus.Cancelled)
            {
                return BadRequest(
                    "A cancelled duty cannot be checked in to.");
            }

            if (dutyTask.Status == DutyStatus.Completed)
            {
                return BadRequest(
                    "A completed duty cannot be checked in to.");
            }

            if (dutyTask.Status == DutyStatus.InProgress)
            {
                return Conflict(
                    "You are already checked in to this duty.");
            }

            dutyTask.Status = DutyStatus.InProgress;

            var parentUserId = dutyTask.ParentProfile?.UserId;
            var auPairUserId = dutyTask.AuPairProfile?.UserId;

            if (!parentUserId.HasValue || !auPairUserId.HasValue)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "The duty is missing valid parent or au pair user information.");
            }

            var checkInNotification = new NotificationLog
            {
                RecipientUserId = parentUserId.Value,
                SenderUserId = auPairUserId.Value,
                DutyTaskId = dutyTask.Id,
                Title = "Au Pair checked in",
                Message =
                    $"The au pair has manually checked in for duty '{dutyTask.Title}'.",
                Type = NotificationType.ManualCheckIn,
                IsRead = false,
                PushDeliveryStatus = PushDeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(checkInNotification);

            await _context.SaveChangesAsync();

            var parentUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
        user.Id == checkInNotification.RecipientUserId);

            if (parentUser == null)
            {
                checkInNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                checkInNotification.PushError = "Parent user could not be found.";
                await _context.SaveChangesAsync();

                _logger.LogError(
                    "Manual check-in notification {NotificationId} was saved, but parent user {UserId} was not found.",
                    checkInNotification.Id,
                    checkInNotification.RecipientUserId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "The duty was checked in and notification saved, but the parent user could not be found.");
            }

            if (string.IsNullOrWhiteSpace(parentUser.OneSignalPlayerId))
            {
                checkInNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                checkInNotification.PushError =
                    "Parent user has no registered OneSignal subscription.";
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Manual check-in notification {NotificationId} was saved, but parent user {UserId} has no OneSignal subscription registered.",
                    checkInNotification.Id,
                    parentUser.Id);

                return Conflict(
                    "The duty was checked in and notification saved, but the parent has no registered OneSignal device.");
            }

            try
            {
                await _oneSignalService.SendPushNotificationAsync(
                    parentUser.OneSignalPlayerId,
                    checkInNotification.Title,
                    checkInNotification.Message,
                    checkInNotification.Id,
                    dutyTask.Id);

                checkInNotification.PushDeliveryStatus = PushDeliveryStatus.Sent;
                checkInNotification.PushSentAt = DateTime.UtcNow;
                checkInNotification.PushError = null;

                await _context.SaveChangesAsync();
            }
            catch (HttpRequestException exception)
            {
                checkInNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                checkInNotification.PushError = exception.Message;
                await _context.SaveChangesAsync();

                _logger.LogError(
                    exception,
                    "Manual check-in notification {NotificationId} was saved, but OneSignal delivery failed.",
                    checkInNotification.Id);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "The duty was checked in and notification saved, but OneSignal delivery failed.");
            }
            catch (InvalidOperationException exception)
            {
                checkInNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                checkInNotification.PushError = exception.Message;
                await _context.SaveChangesAsync();

                _logger.LogError(
                    exception,
                    "Manual check-in notification {NotificationId} was saved, but OneSignal configuration is missing.",
                    checkInNotification.Id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "The duty was checked in and notification saved, but OneSignal is not configured.");
            }

            return Ok(dutyTask.ToResponseDto());
        }

        // POST: api/duties/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteDuty(int id)
        {
            if (!TryGetAuthenticatedUserId(out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.AuPairProfile)
                .FirstOrDefaultAsync(duty => duty.Id == id);

            if (dutyTask == null)
            {
                return NotFound($"Duty task with ID {id} was not found.");
            }

            if (dutyTask.AuPairProfile?.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned au pair can complete this duty.");
            }

            if (dutyTask.Status == DutyStatus.Cancelled)
            {
                return BadRequest(
                    "A cancelled duty cannot be completed.");
            }

            if (dutyTask.Status == DutyStatus.Completed)
            {
                return Conflict(
                    "This duty has already been completed.");
            }

            if (dutyTask.Status == DutyStatus.Pending)
            {
                return BadRequest(
                    "The duty must be checked in before it can be completed.");
            }

            dutyTask.Status = DutyStatus.Completed;

            await _context.SaveChangesAsync();

            return Ok(dutyTask.ToResponseDto());
        }
    }
}