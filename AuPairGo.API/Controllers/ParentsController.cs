using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.DTOs;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParentsController : ControllerBase
    {
        public readonly ApplicationDbContext _context;
        public ParentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Post: api/parents/children
        [HttpPost("children")]
        public async Task<IActionResult> AddChild([FromBody] AddChildDto dto)
        {
            //verify parent exists and has parents role
            var ParentUser = await _context.Users.FirstOrDefaultAsync(user => user.Id == dto.UserId && user.Role == "Parent");

            if (ParentUser == null)
            {
                return BadRequest("Specified parent account was not found or invalid.");
            }

            var child = new ChildProfile
            {
                UserId = dto.UserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Allergies = dto.Allergies,
                SchoolName = dto.SchoolName,
                SpecialInstructions = dto.SpecialInstructions
            };

            _context.ChildProfiles.Add(child);
            await _context.SaveChangesAsync();

            var response = new ChildResponseDto
            {
                Id = child.Id,
                UserId = child.UserId,
                FirstName = child.FirstName,
                LastName = child.LastName,
                Allergies = child.Allergies,
                SchoolName = child.SchoolName,
                SpecialInstructions = child.SpecialInstructions
            };
            return CreatedAtAction(nameof(GetChildrenByParent), new { ParentUserId = dto.UserId }, response);
        }

        //Get: api/parents/{ParentUserId}/children
        [HttpGet("{ParentUserId}/children")]
        public async Task<IActionResult> GetChildrenByParent(int ParentUserId)
        {
            var children = await _context.ChildProfiles.Where(child => child.UserId == ParentUserId).Select(child => new ChildResponseDto
            {
                Id = child.Id,
                UserId = child.UserId,
                FirstName = child.FirstName,
                LastName = child.LastName,
                Allergies = child.Allergies,
                SchoolName = child.SchoolName,
                SpecialInstructions = child.SpecialInstructions
            }).ToListAsync();

            return Ok(children);
        }

        //Put: Api/parents/children/{Id}
        [HttpPut("children/{id}")]
        public async Task<IActionResult> UpdateChild(int id, [FromBody] UpdateChildDto dto)
        {
            //locate existing child profile
            var child = await _context.ChildProfiles.FirstOrDefaultAsync(child => child.Id == id);

            if (child == null)
            {
                return NotFound($"Child Profile with ID: {id} was not found");
            }

            // update properties
            child.FirstName = dto.FirstName;
            child.LastName = dto.LastName;
            child.Allergies = dto.Allergies;
            child.SchoolName = dto.SchoolName;
            child.SpecialInstructions = dto.SpecialInstructions;

            //save updates
            await _context.SaveChangesAsync();

            //return updated record DTO
            var response = new ChildResponseDto
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

        //Delete: api/parents/children/{id}
        [HttpDelete("children/{id}")]
        public async Task<IActionResult> DeleteChild(int id)
        {
            //find child profile
            var child = await _context.ChildProfiles.FirstOrDefaultAsync(child => child.Id == id);

            if (child == null)
            {
                return NotFound($"Child profile with ID {id} was not found.");
            }


            //mark as soft deleted with timestamp
            child.IsDeleted = true;
            child.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}