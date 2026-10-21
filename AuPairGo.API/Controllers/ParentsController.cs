using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParentsController : ControllerBase
    {
        public readonly ApplicationDbContext _context;

        public ParentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Post: api/parents
        [HttpPost]
        public async Task<IActionResult> CreateParentProfile([FromBody] CreateParentProfileDto dto)
        {
            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId != dto.UserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only create a profile for your own account.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == dto.UserId && user.Role == "Parent");
            if (user == null)
            {
                return NotFound($"User with ID {dto.UserId} does not exist.");
            }

            var ExistingProfile = await _context.ParentProfiles
            .FirstOrDefaultAsync(parent => parent.UserId == dto.UserId);

            if (ExistingProfile != null)
            {
                return BadRequest($"Parent Profile already exists for User Id {dto.UserId}.");
            }

            var ParentProfile = new ParentProfile
            {
                UserId = dto.UserId,
                Bio = dto.Bio,
                NumberOfChildren = dto.NumberOfChildren,
                ChildAgeGroups = dto.ChildAgeGroups,
                Address = dto.Address,
                Suburb = dto.Suburb
            };

            _context.ParentProfiles.Add(ParentProfile);
            await _context.SaveChangesAsync();

            var response = new ParentProfileResponseDto
            {
                Id = ParentProfile.Id,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                CellNumber = user.CellNumber,
                Bio = ParentProfile.Bio,
                NumberOfChildren = ParentProfile.NumberOfChildren,
                ChildAgeGroups = ParentProfile.ChildAgeGroups,
                Address = ParentProfile.Address,
                Suburb = ParentProfile.Suburb
            };

            return CreatedAtAction(nameof(GetParentProfile), new { id = ParentProfile.Id }, response);
        }

        //Get: api/parents/{id}     
        [HttpGet("{id}")]
        public async Task<IActionResult> GetParentProfile(int Id)
        {
            var parent = await _context.ParentProfiles
            .Include(parent => parent.User).FirstOrDefaultAsync(parent => parent.Id == Id);

            if (parent == null)
            {
                return NotFound($"Parent Profile with Id {Id} was not found.");
            }

            var response = new ParentProfileResponseDto
            {
                Id = parent.Id,
                UserId = parent.UserId,
                FirstName = parent.User?.FirstName ?? string.Empty,
                LastName = parent.User?.LastName ?? string.Empty,
                Email = parent.User?.Email ?? string.Empty,
                CellNumber = parent.User?.CellNumber ?? string.Empty,
                Bio = parent.Bio,
                NumberOfChildren = parent.NumberOfChildren,
                ChildAgeGroups = parent.ChildAgeGroups,
                Address = parent.Address,
                Suburb = parent.Suburb
            };
            return Ok(response);
        }

        //Put: api/parents/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateParentProfile(int Id, [FromBody] UpdateParentProfileDto dto)
        {
            var parent = await _context.ParentProfiles.Include(parent => parent.User).FirstOrDefaultAsync(parent => parent.Id == Id);

            if (parent == null)
            {
                return NotFound($"Parent profile with Id {Id} was not found.");
            }

            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (parent.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only update your own parent profile.");
            }

            parent.NumberOfChildren = dto.NumberOfChildren;
            parent.ChildAgeGroups = dto.ChildAgeGroups;
            parent.Address = dto.Address;

            if (dto.Suburb != null)
            {
                parent.Suburb = dto.Suburb;
            }

            if (dto.Bio != null)
            {
                parent.Bio = dto.Bio;
            }

            await _context.SaveChangesAsync();

            var response = new ParentProfileResponseDto
            {
                Id = parent.Id,
                UserId = parent.UserId,
                FirstName = parent.User?.FirstName ?? string.Empty,
                LastName = parent.User?.LastName ?? string.Empty,
                Email = parent.User?.Email ?? string.Empty,
                CellNumber = parent.User?.CellNumber ?? string.Empty,
                Bio = parent.Bio,
                NumberOfChildren = parent.NumberOfChildren,
                ChildAgeGroups = parent.ChildAgeGroups,
                Address = parent.Address,
                Suburb = parent.Suburb
            };

            return Ok(response);

        }
    }
}