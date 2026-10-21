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
    [Route("Api/[controller]")]
    [Authorize]
    public class AuPairsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuPairsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Post: api/aupairs
        [HttpPost]
        public async Task<IActionResult> CreateAuPairProfile([FromBody] CreateAuPairProfileDto dto)
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

            var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == dto.UserId && user.Role == "AuPair");
            if (user == null)
            {
                return NotFound($"User with ID {dto.UserId} does not exist.");
            }

            var ExistingProfile = await _context.AuPairProfiles
            .FirstOrDefaultAsync(AuPair => AuPair.UserId == dto.UserId);

            if (ExistingProfile != null)
            {
                return BadRequest($"Au Pair Profile already exists for User Id {dto.UserId}.");
            }

            var AuPairProfile = new AuPairProfile
            {
                UserId = dto.UserId,
                Bio = dto.Bio,
                Suburb = dto.Suburb,
                HasDriversLicense = dto.HasDriversLicense,
                HasCar = dto.HasCar,
                HasFirstAid = dto.HasFirstAid,
                YearsOfExperience = dto.YearsOfExperience,
                Availability = dto.Availability
            };

            _context.AuPairProfiles.Add(AuPairProfile);
            await _context.SaveChangesAsync();

            var response = new AuPairProfileResponseDto
            {
                Id = AuPairProfile.Id,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                CellNumber = user.CellNumber,
                Bio = AuPairProfile.Bio,
                Suburb = AuPairProfile.Suburb ?? string.Empty,
                HasDriversLicense = AuPairProfile.HasDriversLicense,
                HasCar = AuPairProfile.HasCar,
                HasFirstAid = AuPairProfile.HasFirstAid,
                YearsOfExperience = AuPairProfile.YearsOfExperience,
                Availability = AuPairProfile.Availability
            };

            return CreatedAtAction(nameof(GetAuPairProfile), new { Id = AuPairProfile.Id }, response);
        }

        //Get: api/aupairs/{id}
        [HttpGet("{Id}")]
        public async Task<IActionResult> GetAuPairProfile(int Id)
        {
            var AuPair = await _context.AuPairProfiles
            .Include(AuPair => AuPair.User).FirstOrDefaultAsync(AuPair => AuPair.Id == Id);

            if (AuPair == null)
            {
                return NotFound($"Au Pair Profile with Id {Id} was not found.");
            }

            var response = new AuPairProfileResponseDto
            {
                Id = AuPair.Id,
                UserId = AuPair.UserId,
                FirstName = AuPair.User?.FirstName ?? string.Empty,
                LastName = AuPair.User?.LastName ?? string.Empty,
                Email = AuPair.User?.Email ?? string.Empty,
                CellNumber = AuPair.User?.CellNumber ?? string.Empty,
                Bio = AuPair.Bio,
                Suburb = AuPair.Suburb ?? string.Empty,
                HasDriversLicense = AuPair.HasDriversLicense,
                HasCar = AuPair.HasCar,
                HasFirstAid = AuPair.HasFirstAid,
                YearsOfExperience = AuPair.YearsOfExperience,
                Availability = AuPair.Availability
            };

            return Ok(response);

        }

        //Put: api/aupairs/{id}
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateAuPairProfile(int id, [FromBody] UpdateAuPairProfileDto dto)
        {
            var AuPair = await _context.AuPairProfiles.Include(aupair => aupair.User).FirstOrDefaultAsync(aupair => aupair.Id == id);

            if (AuPair == null)
            {
                return NotFound($"Au Pair Profile with id {id} was not found.");
            }

            var authenticatedUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(authenticatedUserIdValue, out var authenticatedUserId))
            {
                return Unauthorized("The authenticated user ID is missing or invalid.");
            }

            if (AuPair.UserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only update your own au-pair profile.");
            }

            AuPair.Bio = dto.Bio;
            AuPair.Suburb = dto.Suburb;
            AuPair.HasDriversLicense = dto.HasDriversLicense;
            AuPair.HasCar = dto.HasCar;
            AuPair.HasFirstAid = dto.HasFirstAid;
            AuPair.YearsOfExperience = dto.YearsOfExperience;
            AuPair.Availability = dto.Availability;

            await _context.SaveChangesAsync();

            var response = new AuPairProfileResponseDto
            {
                Id = AuPair.Id,
                UserId = AuPair.UserId,
                FirstName = AuPair.User?.FirstName ?? string.Empty,
                LastName = AuPair.User?.LastName ?? string.Empty,
                Email = AuPair.User?.Email ?? string.Empty,
                CellNumber = AuPair.User?.CellNumber ?? string.Empty,
                Bio = AuPair.Bio,
                Suburb = AuPair.Suburb ?? string.Empty,
                HasDriversLicense = AuPair.HasDriversLicense,
                HasCar = AuPair.HasCar,
                HasFirstAid = AuPair.HasFirstAid,
                YearsOfExperience = AuPair.YearsOfExperience,
                Availability = AuPair.Availability
            };

            return Ok(response);
        }
    }
}