using Microsoft.AspNetCore.Mvc;
using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // This makes the URL: api/auth
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Inject our database gateway into the controller
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")] // URL: api/auth/register
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. Check if the email is already taken
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("A user with this email already exists.");
            }

            // 2. Hash the password using BCrypt before storing
            string HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Map the DTO data into our database User model
            var newUser = new User
            {
                Email = dto.Email,
                Password = HashedPassword,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CellNumber = dto.CellNumber,
                IdNumber = dto.IdNumber,
                Role = dto.Role
            };

            // 3. Save to MySQL via EF Core
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully and password encrypted!");
        }
    }
}