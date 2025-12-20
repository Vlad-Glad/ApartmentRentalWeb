using ApartmentRental.Data;
using ApartmentRental.Models;
using ApartmentRental.Models.DTO;
using ApartmentRental.Search;
using ApartmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly IGeocodingService _geocodingService;
        private readonly IApartmentSearchService _search;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApartmentsController> _logger;

        public ApartmentsController(
            ApplicationDbContext context,
            IGeocodingService geocodingService,
            IApartmentSearchService search,
            UserManager<ApplicationUser> userManager,
            ILogger<ApartmentsController> logger)
        {
            _context = context;
            _geocodingService = geocodingService;
            _search = search;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResultDto<ApartmentDto>>> GetApartments(
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 10)
        {
            if (limit <= 0) limit = 10;
            if (limit > 100) limit = 100;

            var baseQuery = _context.Apartments.AsNoTracking();

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderBy(a => a.Id)
                .Skip(skip)
                .Take(limit)
                .Select(a => new ApartmentDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    City = a.City,
                    Price = a.Price,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                })
                .ToListAsync();

            string? nextLink = null;

            if (skip + limit < totalCount)
            {
                var nextSkip = skip + limit;

                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.Path}";

                nextLink = $"{baseUrl}?skip={nextSkip}&limit={limit}";
            }

            return Ok(new PagedResultDto<ApartmentDto>
            {
                Items = items,
                TotalCount = totalCount,
                Skip = skip,
                Limit = limit,
                NextLink = nextLink
            });
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

            if (apartment == null) return NotFound();
            return Ok(apartment);
        }

        [HttpPost]
        public async Task<IActionResult> CreateApartment([FromBody] ApartmentCreateDto dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest(new { message = "Request body is required." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            dto.FullAddress = dto.FullAddress.Trim();

            var duplicateExists = await _context.Apartments.AnyAsync(a =>
                a.LessorId == userId &&
                a.FullAddress == dto.FullAddress, ct);

            if (duplicateExists)
                return Conflict(new { message = "An apartment with the same address already exists." });

            if (string.IsNullOrWhiteSpace(dto.City) || dto.Latitude == null || dto.Longitude == null)
            {
                if (string.IsNullOrWhiteSpace(dto.FullAddress))
                    return BadRequest(new { message = "FullAddress is required when City/Latitude/Longitude are not provided." });

                var suggestions = await _geocodingService.SearchAsync(dto.FullAddress, limit: 1);

                var first = suggestions.FirstOrDefault();
                if (first == null)
                {
                    return BadRequest(new
                    {
                        message = "Failed to resolve address using geocoder. уточніть FullAddress або вкажіть City/Latitude/Longitude вручну."
                    });
                }

                dto.City = first.City;
                dto.Latitude = first.Latitude;
                dto.Longitude = first.Longitude;
                dto.FullAddress = first.Label;
            }

            var apartment = new Apartment
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                City = dto.City!,
                FullAddress = dto.FullAddress,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                LessorId = userId,
            };

            _context.Apartments.Add(apartment);
            await _context.SaveChangesAsync(ct);

            // Index in Azure Search (best-effort)
            await TryIndexAsync(apartment, ct);

            return CreatedAtAction(nameof(GetApartment), new { id = apartment.Id }, new { apartment.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateApartment(int id, [FromBody] ApartmentUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var apartment = await _context.Apartments.FindAsync(new object[] { id }, ct);
            if (apartment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            // (Optional but recommended) authorize ownership
            if (apartment.LessorId != userId) return Forbid();

            if (dto.FullAddress != null)
            {
                var newAddress = dto.FullAddress.Trim();

                var duplicateExists = await _context.Apartments.AnyAsync(a =>
                    a.Id != id &&
                    a.LessorId == userId &&
                    a.FullAddress == newAddress, ct);

                if (duplicateExists)
                    return Conflict(new { message = "An apartment with the same address already exists." });

                apartment.FullAddress = newAddress;
            }

            if (dto.Title != null) apartment.Title = dto.Title;
            if (dto.Description != null) apartment.Description = dto.Description;
            if (dto.Price != null) apartment.Price = dto.Price.Value;
            if (dto.City != null) apartment.City = dto.City;
            if (dto.Latitude != null) apartment.Latitude = dto.Latitude;
            if (dto.Longitude != null) apartment.Longitude = dto.Longitude;

            await _context.SaveChangesAsync(ct);

            // Re-index updated doc
            await TryIndexAsync(apartment, ct);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteApartment(int id, CancellationToken ct)
        {
            var apartment = await _context.Apartments.FindAsync(new object[] { id }, ct);
            if (apartment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            // (Optional but recommended) authorize ownership
            if (apartment.LessorId != userId) return Forbid();

            _context.Apartments.Remove(apartment);
            await _context.SaveChangesAsync(ct);

            // Remove from Azure Search
            await TryDeleteFromIndexAsync(id, ct);

            return NoContent();
        }

        private async Task TryIndexAsync(Apartment apartment, CancellationToken ct)
        {
            try
            {
                var lessor = await _userManager.FindByIdAsync(apartment.LessorId);
                var lessorEmail = lessor?.Email ?? "";

                var doc = new ApartmentSearchDocument
                {
                    Id = $"apt-{apartment.Id}",
                    ApartmentId = apartment.Id,
                    Title = apartment.Title ?? "",
                    LessorEmail = lessorEmail
                };

                await _search.IndexAsync(doc, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Search indexing failed for apartment {ApartmentId}", apartment.Id);
            }
        }

        private async Task TryDeleteFromIndexAsync(int apartmentId, CancellationToken ct)
        {
            try
            {
                await _search.DeleteAsync($"apt-{apartmentId}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Search delete failed for apartment {ApartmentId}", apartmentId);
            }
        }
    }
}
