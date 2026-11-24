using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Dtos;

namespace ReturnFlow.Api.Services;

public class TokenService(IConfiguration config)
{
    public AuthResponse Create(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString())
            }),
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new AuthResponse
        {
            Token = new JsonWebTokenHandler().CreateToken(descriptor),
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}
