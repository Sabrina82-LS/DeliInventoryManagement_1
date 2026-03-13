using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeliInventoryManagement_1.Api.ModelsV5.Auth;
using Microsoft.IdentityModel.Tokens;

namespace DeliInventoryManagement_1.Api.Services.Auth;

public class JwtTokenService(IConfiguration config)
{
    public string CreateToken(AppUser user)
    {
        var jwtSection = config.GetSection("Jwt");
        var issuer = jwtSection["Issuer"]!;
        var audience = jwtSection["Audience"]!;
        var key = jwtSection["Key"]!;
        var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "120");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}