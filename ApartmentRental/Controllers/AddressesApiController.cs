using ApartmentRental.Models.DTO;
using ApartmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentRental.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AddressesController : ControllerBase
    {
        private readonly IGeocodingService _geocodingService;

        public AddressesController(IGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<AddressSuggestionDto>>> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
                return Ok(new List<AddressSuggestionDto>());

            var suggestions = await _geocodingService.SearchAsync(q, limit: 5);
            return Ok(suggestions);
        }
    }
}
