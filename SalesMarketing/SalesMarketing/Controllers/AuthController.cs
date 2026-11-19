
using Microsoft.AspNetCore.Mvc;
using SalesMarketing.DTOs;
using SalesMarketing.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace SalesMarketing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);

            return Ok(response);
        }
    }
}
