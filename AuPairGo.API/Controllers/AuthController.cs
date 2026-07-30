using Microsoft.AspNetCore.Mvc;
using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. Check if the email is already taken
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("A user with this email already exists.");
            }

            // 2. Hash the password using BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Begin database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 4. Map DTO to User model
                var newUser = new User
                {
                    Email = dto.Email,
                    Password = hashedPassword,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    CellNumber = dto.CellNumber,
                    IdNumber = dto.IdNumber,
                    Role = dto.Role
                };

                _context.Users.Add(newUser);

                // 5. Initialize profile stub matching exact model properties
                if (newUser.Role == "AuPair")
                {
                    var auPairProfile = new AuPairProfile
                    {
                        User = newUser,
                        Bio = "Not Available",
                        Suburb = "Not Available",
                        HasDriversLicense = false,
                        HasCar = false,
                        HasFirstAid = false,
                        YearsOfExperience = 0,
                        Availability = "Not set"
                    };
                    _context.AuPairProfiles.Add(auPairProfile);
                }
                else if (newUser.Role == "Parent")
                {
                    var parentProfile = new ParentProfile
                    {
                        User = newUser,
                        Bio = string.Empty,
                        ChildAgeGroups = string.Empty,
                        NumberOfChildren = 0
                    };
                    _context.ParentProfiles.Add(parentProfile);
                }
                else
                {
                    return BadRequest("Invalid role assignment specified.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok("User and profile records initialized successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"An error occurred during account creation: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 1. Find user by email
            var userRecord = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (userRecord == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            // 2. Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, userRecord.Password);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid email or password.");
            }

            // 3. Fetch profile based on role
            object? profileData = null;

            if (userRecord.Role == "AuPair")
            {
                var auPairProfile = await _context.AuPairProfiles.FirstOrDefaultAsync(p => p.UserId == userRecord.Id);
                if (auPairProfile != null)
                {
                    profileData = new
                    {
                        auPairProfile.Id,
                        auPairProfile.Bio,
                        auPairProfile.Suburb,
                        auPairProfile.HasFirstAid,
                        auPairProfile.YearsOfExperience,
                        auPairProfile.HasDriversLicense,
                        auPairProfile.HasCar,
                        auPairProfile.Availability
                    };
                }
            }
            else if (userRecord.Role == "Parent")
            {
                var parentProfile = await _context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == userRecord.Id);
                if (parentProfile != null)
                {
                    profileData = new
                    {
                        parentProfile.Id,
                        parentProfile.Bio,
                        parentProfile.ChildAgeGroups,
                        parentProfile.NumberOfChildren,
                        parentProfile.Address,
                        parentProfile.Suburb
                    };
                }
            }

            // 4. Return user and profile details
            return Ok(new
            {
                userRecord.Id,
                userRecord.Email,
                userRecord.FirstName,
                userRecord.LastName,
                userRecord.CellNumber,
                userRecord.IdNumber,
                userRecord.Role,
                Profile = profileData
            });
        }
    }
}