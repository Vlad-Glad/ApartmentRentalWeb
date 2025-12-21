using ApartmentRental.Data;
using ApartmentRental.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRental.Controllers.Api;

[ApiController]
[Route("api/stats")]
public sealed class StatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public StatsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/stats/apartments
    [HttpGet("apartments")]
    [AllowAnonymous]
    public async Task<ActionResult<ApartmentStatsDto>> GetApartmentsStats(CancellationToken ct)
    {
        var baseQuery = _context.Apartments.AsNoTracking();

        var total = await baseQuery.CountAsync(ct);

        var byCity = await baseQuery
            .GroupBy(a => string.IsNullOrWhiteSpace(a.City) ? "Unknown" : a.City!)
            .Select(g => new CityStatDto
            {
                City = g.Key,
                Count = g.Count(),
                AvgPrice = Math.Round(g.Average(x => (decimal)x.Price), 0)
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        return Ok(new ApartmentStatsDto
        {
            TotalApartments = total,
            ByCity = byCity
        });
    }
}
