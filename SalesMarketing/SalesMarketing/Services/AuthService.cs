
using Microsoft.IdentityModel.Tokens;
using SalesMarketing.DTOs;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SalesMarketing.Exceptions;

namespace SalesMarketing.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(
            LoginRequestDto request)
        {
            // Find admin by username.
            var user = await _authRepository
                .GetUserByUsernameAsync(request.Username);

            // Validate username and password.
            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash))
            {
                _logger.LogWarning(
                    "Failed login attempt for username {Username}.",
                    request.Username);

                throw new UnauthorizedAccessException(
                    "Invalid username or password.");
            }

            // Validate admin role.
            if (user.Adminrole != 1 && user.Adminrole != 2)
            {
                _logger.LogWarning(
                    "Login rejected because user {UserId} has an invalid admin role.",
                    user.Id);

                throw new UnauthorizedAccessException(
                    "Invalid admin role.");
            }

            // Generate JWT token.
            var token = GenerateJwtToken(
                user.Id,
                user.Username,
                user.Adminrole);

            _logger.LogInformation(
                "User {UserId} logged in successfully with admin role {AdminRole}.",
                user.Id,
                user.Adminrole);

            return new LoginResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Adminrole = user.Adminrole,
                Token = token,
                Message = "Login successful."
            };
        }

        private string GenerateJwtToken(
            long userId,
            string username,
            short adminRole)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key is missing.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is missing.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is missing.");

            var expiryInMinutes =
                _configuration.GetValue<int>("Jwt:ExpiryInMinutes");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    username),

                new Claim(
                    ClaimTypes.Role,
                    adminRole.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
