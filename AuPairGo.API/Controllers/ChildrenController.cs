using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuPairGo.API.DTOs;
using AuPairGo.API.Data;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/parents/{ParentId}/children")]
    [Authorize]
    public class ChildrenController : ControllerBase
    {
        public readonly ApplicationDbContext _context;

        public ChildrenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/parents/{ParentId}/children
        [HttpPost]
        public async Task<IActionResult> CreateChildProfile(
            int ParentId,
            [FromBody] CreateChildProfileDto dto)
        {
            var authenticatedUserIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                    authenticatedUserIdValue,
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var parentProfile = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(parent =>
                    parent.Id == ParentId &&
                    parent.UserId == authenticatedUserId);

            if (parentProfile == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only create children for your own parent profile.");
            }

            var child = new ChildProfile
            {
                UserId = parentProfile.UserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Allergies = dto.Allergies,
                SchoolName = dto.SchoolName,
                SpecialInstructions = dto.SpecialInstructions
            };

            _context.ChildProfiles.Add(child);
            await _context.SaveChangesAsync();

            var response = new ChildProfileResponseDto
            {
                Id = child.Id,
                UserId = child.UserId,
                FirstName = child.FirstName,
                LastName = child.LastName,
                Allergies = child.Allergies,
                SchoolName = child.SchoolName,
                SpecialInstructions = child.SpecialInstructions
            };

            return CreatedAtAction(
                nameof(GetChildById),
                new { ParentId, ChildId = child.Id },
                response);
        }

        // GET: api/parents/{ParentId}/children
        [HttpGet]
        public async Task<IActionResult> GetChildrenByParent(int ParentId)
        {
            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId != ParentId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only view children belonging to your own account.");
            }

            var children = await _context.ChildProfiles
                .Where(child => child.UserId == ParentId && !child.IsDeleted) // Included !IsDeleted
                .Select(child => new ChildProfileResponseDto
                {
                    Id = child.Id,
                    UserId = child.UserId,
                    FirstName = child.FirstName,
                    LastName = child.LastName,
                    Allergies = child.Allergies,
                    SchoolName = child.SchoolName,
                    SpecialInstructions = child.SpecialInstructions
                })
                .ToListAsync();

            return Ok(children);
        }

        // GET: api/parents/{ParentId}/children/{ChildId}
        [HttpGet("{ChildId}")]
        public async Task<IActionResult> GetChildById(int ParentId, int ChildId)
        {
            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId != ParentId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only view children belonging to your own account.");
            }

            var child = await _context.ChildProfiles
                .Where(child => child.Id == ChildId && child.UserId == ParentId && !child.IsDeleted)
                .Select(child => new ChildProfileResponseDto
                {
                    Id = child.Id,
                    UserId = child.UserId,
                    FirstName = child.FirstName,
                    LastName = child.LastName,
                    Allergies = child.Allergies,
                    SchoolName = child.SchoolName,
                    SpecialInstructions = child.SpecialInstructions
                })
                .FirstOrDefaultAsync();

            if (child == null)
            {
                return NotFound($"Child profile with the Id {ChildId} could not be found for parent Id {ParentId}.");
            }

            return Ok(child);
        }

        // PUT: api/parents/{ParentId}/children/{ChildId}
        [HttpPut("{ChildId}")]
        public async Task<IActionResult> UpdateChildProfile(int ParentId, int ChildId, [FromBody] UpdateChildProfileDto dto)
        {
            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId != ParentId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only modify children belonging to your own account.");
            }

            var child = await _context.ChildProfiles
                .FirstOrDefaultAsync(c => c.Id == ChildId && c.UserId == ParentId && !c.IsDeleted);

            if (child == null)
            {
                return NotFound($"Child Profile with ID {ChildId} was not found for parent ID {ParentId}.");
            }

            // Required fields (always updated from valid payload)
            child.FirstName = dto.FirstName;
            child.LastName = dto.LastName;
            child.SchoolName = dto.SchoolName;

            // Optional fields: Only overwrite if the caller provided a non-null string
            if (dto.Allergies != null)
            {
                child.Allergies = dto.Allergies;
            }

            if (dto.SpecialInstructions != null)
            {
                child.SpecialInstructions = dto.SpecialInstructions;
            }

            await _context.SaveChangesAsync();

            var response = new ChildProfileResponseDto
            {
                Id = child.Id,
                UserId = child.UserId,
                FirstName = child.FirstName,
                LastName = child.LastName,
                Allergies = child.Allergies,
                SchoolName = child.SchoolName,
                SpecialInstructions = child.SpecialInstructions
            };

            return Ok(response);
        }

        // DELETE: api/parents/{ParentId}/children/{ChildId}
        [HttpDelete("{ChildId}")]
        public async Task<IActionResult> DeleteChild(int ParentId, int ChildId)
        {
            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId != ParentId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only modify children belonging to your own account.");
            }

            var child = await _context.ChildProfiles
                .FirstOrDefaultAsync(c => c.Id == ChildId && c.UserId == ParentId && !c.IsDeleted);

            if (child == null)
            {
                return NotFound($"Child profile with ID {ChildId} was not found for parent with parent ID {ParentId}.");
            }

            // Mark as soft deleted with timestamp
            child.IsDeleted = true;
            child.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}