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

            // 3. begin database transaction for data safety
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                // 4. Map the DTO data into our database User model
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

                //5. Check the string value of the role and initialize the matching profile
                if (newUser.Role == "AuPair")
                {
                    var AuPairProfile = new AuPairProfile
                    {
                        User = newUser,
                        Bio = string.Empty,
                        Experience = string.Empty,
                        HourlyRate = 0.00m,
                        HasDriversLicense = false
                    };
                    _context.AuPairProfiles.Add(AuPairProfile);
                }
                else if (newUser.Role == "Parent")
                {
                    var ParentProfile = new ParentProfile
                    {
                        User = newUser,
                        Bio = string.Empty,
                        ChildAgeGroups = string.Empty,
                        NumberOfChildren = 0
                    };
                    _context.ParentProfiles.Add(ParentProfile);
                }
                else
                {
                    return BadRequest("Invalid role assignment specified.");
                }

                // save the profile record 
                await _context.SaveChangesAsync();

                // Commit the transaction to save both tables together
                await transaction.CommitAsync();

                return Ok("User and profile records initialized successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occured during account creation processing.");
            }
        }

        [HttpPost("login")] // URL: api/auth/register
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            //1. look for the user in the database by email
            var UserRecord = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            // 2. if user doesnt exist, return a generic error message
            if (UserRecord == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            // 3. if user exists, verify the password using BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, UserRecord.Password);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid email or password.");
            }

            // 4. Fetch the specialized profile depending on the logged in role
            object? ProfileData = null;
            if (UserRecord.Role == "AuPair")
            {
                // Query the table using users ID
                var AuPairProfile = await _context.AuPairProfiles.FirstOrDefaultAsync(p => p.UserId == UserRecord.Id);

                // If it exists, map the fields into the profile object
                if (AuPairProfile != null)
                {
                    ProfileData = new { AuPairProfile.Bio, AuPairProfile.HourlyRate, AuPairProfile.Experience, AuPairProfile.HasDriversLicense };
                }
            }
            else if (UserRecord.Role == "Parent")
            {
                var ParentProfile = await _context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == UserRecord.Id);

                if (ParentProfile != null)
                {
                    ProfileData = new { ParentProfile.Bio, ParentProfile.ChildAgeGroups, ParentProfile.NumberOfChildren };
                }
            }

            // 5. return core user details and profile payload attached(we will upgrade this to hand back a secure JWT token)
            return Ok(new
            {
                UserRecord.Id,
                UserRecord.Email,
                UserRecord.FirstName,
                UserRecord.LastName,
                UserRecord.CellNumber,
                UserRecord.IdNumber,
                UserRecord.Role,
                Profile = ProfileData
            });
        }
    }
}