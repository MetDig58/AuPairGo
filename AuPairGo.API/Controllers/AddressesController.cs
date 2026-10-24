using System.Security.Claims;
using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/addresses")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress(
            [FromBody] CreateAddressDto dto)
        {
            if (!TryGetAuthenticatedUserId(out var userId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var parent = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(profile => profile.UserId == userId);

            if (parent == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only a parent account can manage addresses.");
            }

            var address = new Address
            {
                ParentProfileId = parent.Id,
                StreetAddress = dto.StreetAddress,
                Suburb = dto.Suburb,
                City = dto.City,
                PostalCode = dto.PostalCode,
                latitude = dto.latitude,
                longitude = dto.longitude
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAddress),
                new { id = address.Id },
                address.ToResponseDto());
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            if (!TryGetAuthenticatedUserId(out var userId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var parent = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(profile => profile.UserId == userId);

            if (parent == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only a parent account can manage addresses.");
            }

            var addresses = await _context.Addresses
                .AsNoTracking()
                .Where(address =>
                    address.ParentProfileId == parent.Id)
                .OrderBy(address => address.Id)
                .ToListAsync();

            return Ok(addresses
                .Select(address => address.ToResponseDto())
                .ToList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddress(int id)
        {
            var address = await GetOwnedAddress(id);

            if (address == null)
            {
                return NotFound(
                    $"Address with ID {id} was not found.");
            }

            return Ok(address.ToResponseDto());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(
            int id,
            [FromBody] UpdateAddressDto dto)
        {
            var address = await GetOwnedAddress(id);

            if (address == null)
            {
                return NotFound(
                    $"Address with ID {id} was not found.");
            }

            address.StreetAddress = dto.StreetAddress;
            address.Suburb = dto.Suburb;
            address.City = dto.City;
            address.PostalCode = dto.PostalCode;
            address.latitude = dto.latitude;
            address.longitude = dto.longitude;

            await _context.SaveChangesAsync();

            return Ok(address.ToResponseDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var address = await GetOwnedAddress(id);

            if (address == null)
            {
                return NotFound(
                    $"Address with ID {id} was not found.");
            }

            var isUsedByDuty = await _context.DutyTasks
                .AsNoTracking()
                .AnyAsync(duty =>
                    duty.PickupAddressId == id ||
                    duty.DropOffAddressId == id);

            if (isUsedByDuty)
            {
                return Conflict(
                    "This address is used by a duty and cannot be deleted.");
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<Address?> GetOwnedAddress(int addressId)
        {
            if (!TryGetAuthenticatedUserId(out var userId))
            {
                return null;
            }

            return await _context.Addresses
                .FirstOrDefaultAsync(address =>
                    address.Id == addressId &&
                    address.ParentProfile != null &&
                    address.ParentProfile.UserId == userId);
        }

        private bool TryGetAuthenticatedUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }
    }
}