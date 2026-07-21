using KT_Learn.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace KT_Learn.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(User user)
        {
            var jwt = _configuration.GetSection("Jwt");

            // Без явной проверки отсутствующий Jwt:Key даёт NullReferenceException
            // где-то в недрах Encoding — по такому сообщению причину не найти.
            var keyValue = jwt["Key"]
                ?? throw new InvalidOperationException("Не задан Jwt:Key в конфигурации.");
            var expiryMinutes = jwt["ExpiryMinutes"]
                ?? throw new InvalidOperationException("Не задан Jwt:ExpiryMinutes в конфигурации.");

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(keyValue));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),   
                new Claim(JwtRegisteredClaimNames.Email, user.Email),        
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(expiryMinutes)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
