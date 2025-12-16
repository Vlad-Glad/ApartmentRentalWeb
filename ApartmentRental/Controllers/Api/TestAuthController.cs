using ApartmentRental.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace ApartmentRental.Controllers.Api
{
    [ApiController]
    [Route("api/test-auth")]
    public class TestAuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public TestAuthController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return NotFound();

            if (dto is null) return BadRequest(new { message = "Request body is required." });

            var result = await _signInManager.PasswordSignInAsync(
                dto.Email, dto.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials." });

            return Ok(new { message = "Logged in." });
        }

        public class LoginDto
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }
    }
}
