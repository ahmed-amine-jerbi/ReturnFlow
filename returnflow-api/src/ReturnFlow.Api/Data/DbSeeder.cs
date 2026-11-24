using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Domain;

namespace ReturnFlow.Api.Data;

/// <summary>Development-only demo users. Never run this in production.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        if (await db.Users.AnyAsync()) return;

        var password = config["Seed:Password"] ?? "ChangeMe123!";

        db.Users.AddRange(
            NewUser("admin@returnflow.local", Role.Admin, password),
            NewUser("manager@returnflow.local", Role.Manager, password),
            NewUser("agent@returnflow.local", Role.Agent, password),
            NewUser("customer@returnflow.local", Role.Customer, password));

        await db.SaveChangesAsync();
    }

    private static AppUser NewUser(string email, Role role, string password) => new()
    {
        Email = email,
        Role = role,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
    };
}
