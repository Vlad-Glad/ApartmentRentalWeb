using ApartmentRental.Data;
using ApartmentRental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApartmentRental.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApartmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ApartmentDto>>> GetApartments()
        {
            var apartments = await _context.Apartments
                .Include(a => a.Lessor)
                .Select(a => new ApartmentDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Price = a.Price,
                    City = a.City,
                    FullAddress = a.FullAddress,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    LessorId = a.LessorId,
                    LessorEmail = a.Lessor != null ? a.Lessor.Email : null
                })
                .ToListAsync();

            return Ok(apartments);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApartmentDto>> GetApartment(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Lessor)
                .Where(a => a.Id == id)
                .Select(a => new ApartmentDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Price = a.Price,
                    City = a.City,
                    FullAddress = a.FullAddress,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    LessorId = a.LessorId,
                    LessorEmail = a.Lessor != null ? a.Lessor.Email : null
                })
                .FirstOrDefaultAsync();

            if (apartment == null)
            {
                return NotFound();
            }

            return Ok(apartment);
        }

        [HttpPost]
        public async Task<IActionResult> CreateApartment([FromBody] ApartmentCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null) return Unauthorized();

            var apartment = new Apartment
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                City = dto.City,
                FullAddress = dto.FullAddress,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                LessorId = userId,
            };

            _context.Apartments.Add(apartment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetApartment), new { id = apartment.Id }, new { apartment.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateApartment(int id, [FromBody] ApartmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var apartment = await _context.Apartments.FindAsync(id);
            if (apartment == null)
            {
                return NotFound();
            }

            if (dto.Title != null) apartment.Title = dto.Title;
            if (dto.Description != null) apartment.Description = dto.Description;
            if (dto.Price != null) apartment.Price = dto.Price.Value;
            if (dto.City != null) apartment.City = dto.City;
            if (dto.FullAddress != null) apartment.FullAddress = dto.FullAddress;
            if (dto.Latitude != null) apartment.Latitude = dto.Latitude;
            if (dto.Longitude != null) apartment.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteApartment(int id)
        {
            var apartment = await _context.Apartments.FindAsync(id);
            if (apartment == null)
            {
                return NotFound();
            }

            _context.Apartments.Remove(apartment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}