using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Dtos;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    /// <summary>Self-registration always creates a Customer. Staff accounts are created by an admin.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { error = "Email already registered." });

        var user = new AppUser
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = Role.Customer
        };
        await db.AddWithAuditAsync(user, u => AuditLog.For(u.Id, "UserRegistered", nameof(AppUser), u.Id));

        return Ok(tokens.Create(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(tokens.Create(user));
    }
}
