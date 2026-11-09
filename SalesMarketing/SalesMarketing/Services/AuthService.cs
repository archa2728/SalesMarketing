using Microsoft.IdentityModel.Tokens;
using SalesMarketing.DTOs;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SalesMarketing.Services
{
    public class AuthService: IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration)
        {
            _authRepository = authRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // Find admin by username
            var user = await _authRepository
                .GetUserByUsernameAsync(request.Username);

            // Validate username and password
            if (user == null || user.Password != request.Password)
            {
                throw new UnauthorizedAccessException(
                    "Invalid username or password.");
            }

            // Validate admin role
            if (user.Adminrole != 1 && user.Adminrole != 2)
            {
                throw new UnauthorizedAccessException(
                    "Invalid admin role.");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Username, user.Adminrole);

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
            var jwtKey = _configuration["Jwt:Key"]!;
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, adminRole.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
