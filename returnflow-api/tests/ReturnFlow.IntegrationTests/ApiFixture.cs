using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Services;
using Testcontainers.PostgreSql;

namespace ReturnFlow.IntegrationTests;

/// <summary>
/// One PostgreSQL container and one running API for all tests in the collection. Startup applies the
/// real migrations, so every test also proves the schema they create works. The AI service URL points
/// at a closed port, so claims take the "AI unavailable, send to an agent" path.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    /// <summary>The API's JSON settings: web defaults with enums as strings.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
            builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");
            builder.UseSetting("Seed:DemoData", "false");
            builder.UseSetting("Ai:BaseUrl", "http://127.0.0.1:9/");
        });

        // Force the host to start now, which applies the migrations.
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>A fresh context of its own, independent of the API's, so tests can stage and inspect data.</summary>
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);

    /// <summary>Creates a user straight in the database and returns a client signed in as them.</summary>
    public async Task<(AppUser User, HttpClient Client)> CreateSignedInUserAsync(Role role)
    {
        var user = new AppUser
        {
            Email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@tests.local",
            PasswordHash = "not-used-by-these-tests",
            Role = role
        };

        await using (var db = CreateDbContext())
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        using var scope = Factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<TokenService>().Create(user).Token;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (user, client);
    }

    /// <summary>Inserts a return with one claim in the given state, without queueing it for the worker.</summary>
    public async Task<ReturnClaim> CreateClaimAsync(int customerId, Action<ReturnClaim>? arrange = null)
    {
        var claim = new ReturnClaim { Description = "The screen arrived cracked", AmountRequested = 60m };
        arrange?.Invoke(claim);

        var ret = new ReturnRequest
        {
            CustomerId = customerId,
            OrderReference = "ORD-TEST",
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            Reason = "Phone",
            Claims = { claim }
        };

        await using var db = CreateDbContext();
        db.Returns.Add(ret);
        await db.SaveChangesAsync();
        return claim;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "API with PostgreSQL";
}
