using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WasteManagement3.Models;

namespace WasteManagement3.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GenerateJwtToken(Users user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var secretKey = _configuration["Jwt:Key"]
                ?? throw new ArgumentNullException("Jwt:SecretKey is missing in configuration.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new ArgumentNullException("Jwt:Issuer is missing in configuration.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new ArgumentNullException("Jwt:Audience is missing in configuration.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

