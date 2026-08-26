using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateToken(AppUser user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is not configured.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var expireMinutes = int.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 480;

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
