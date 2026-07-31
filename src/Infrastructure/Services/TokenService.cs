using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sportner.Application.Abstractions;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string CreateToken(User user)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
        var issuer = jwtSettings["Issuer"] ?? "SportnerApi";
        var audience = jwtSettings["Audience"] ?? "SportnerMobile";
        var expirationDays = int.TryParse(jwtSettings["ExpirationDays"], out var days) ? days : 7;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.FullName));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
