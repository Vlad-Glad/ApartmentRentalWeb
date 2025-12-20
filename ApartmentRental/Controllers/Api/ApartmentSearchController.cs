using ApartmentRental.Data;
using ApartmentRental.Models;
using ApartmentRental.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRental.Controllers.Api
{
    [ApiController]
    [Route("api/search")]
    [AllowAnonymous]
    public sealed class ApartmentSearchController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IApartmentSearchService _search;

        public ApartmentSearchController(ApplicationDbContext db, IApartmentSearchService search)
        {
            _db = db;
            _search = search;
        }

        // GET: /api/search?q=vasylkiv
        [HttpGet]
        public async Task<ActionResult<SearchResponse>> Get(
            [FromQuery] string q,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10,
            [FromQuery] bool includeAzureDocs = false,
            CancellationToken ct = default)
        {
            return Ok(await ExecuteSearchAsync(q, skip, take, includeAzureDocs, ct));
        }

        // POST: /api/search
        [HttpPost]
        public async Task<ActionResult<SearchResponse>> Post([FromBody] SearchRequest request, CancellationToken ct)
        {
            request ??= new SearchRequest();
            return Ok(await ExecuteSearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludeAzureDocs,
                ct));
        }

        private async Task<SearchResponse> ExecuteSearchAsync(
            string? query,
            int skip,
            int take,
            bool includeAzureDocs,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new SearchResponse
                {
                    Query = query ?? "",
                    TotalAzureHits = 0,
                    Items = new List<SearchApartmentDto>()
                };
            }

            if (take <= 0) take = 20;
            if (take > 50) take = 50;
            if (skip < 0) skip = 0;

            var hits = await _search.SearchAsync(query, ct);
            var total = hits.Count;

            var page = hits.Skip(skip).Take(take).ToList();
            var ids = page.Select(x => x.ApartmentId).Distinct().ToList();

            if (ids.Count == 0)
            {
                return new SearchResponse
                {
                    Query = query,
                    TotalAzureHits = total,
                    Items = new List<SearchApartmentDto>(),
                    AzureDocs = includeAzureDocs ? page : null
                };
            }

            var apartments = await _db.Apartments
                .Include(a => a.Lessor)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync(ct);

            var order = ids.Select((id, i) => new { id, i }).ToDictionary(x => x.id, x => x.i);
            apartments = apartments.OrderBy(a => order[a.Id]).ToList();

            var items = apartments.Select(a => new SearchApartmentDto
            {
                ApartmentId = a.Id,
                Title = a.Title ?? "",
                City = a.City,
                Price = a.Price,
                LessorEmail = a.Lessor?.Email,
                LandingUrl = Url.Action("Landing", "Apartments", new { id = a.Id }, Request.Scheme)
            }).ToList();

            return new SearchResponse
            {
                Query = query,
                TotalAzureHits = total,
                Skip = skip,
                Take = take,
                Items = items,
                AzureDocs = includeAzureDocs ? page : null
            };
        }

        public sealed class SearchRequest
        {
            public string? Query { get; set; }
            public int Skip { get; set; } = 0;
            public int Take { get; set; } = 20;
            public bool IncludeAzureDocs { get; set; } = false;
        }

        public sealed class SearchResponse
        {
            public string Query { get; set; } = "";
            public int TotalAzureHits { get; set; }
            public int Skip { get; set; }
            public int Take { get; set; }
            public List<SearchApartmentDto> Items { get; set; } = new();

            public List<ApartmentSearchDocument>? AzureDocs { get; set; }
        }

        public sealed class SearchApartmentDto
        {
            public int ApartmentId { get; set; }
            public string Title { get; set; } = "";
            public string? City { get; set; }
            public decimal Price { get; set; }
            public string? LessorEmail { get; set; }
            public string? LandingUrl { get; set; }
        }
    }
}
