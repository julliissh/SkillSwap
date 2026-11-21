using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Common.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class JwtTokenService
{
    public const int MinKeyBytes = 32;

    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var keyBytes = Convert.FromBase64String(key);
        if (keyBytes.Length < MinKeyBytes)
        {
            throw new InvalidOperationException($"JWT key must be at least {MinKeyBytes} bytes.");
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role)
        };

        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
