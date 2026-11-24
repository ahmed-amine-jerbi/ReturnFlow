using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Services;
using ReturnFlow.Api.Workers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- Auth (JWT) ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

// The key in appsettings.json is public. Anywhere but Development, insist on a real one.
if (!builder.Environment.IsDevelopment()
    && (jwtKey.StartsWith("DEV_ONLY", StringComparison.Ordinal) || Encoding.UTF8.GetByteCount(jwtKey) < 32))
{
    throw new InvalidOperationException("Set Jwt:Key (env Jwt__Key) to a random secret of at least 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim names exactly as written in the token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "email",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// --- Automation: AI client, policy, queue, background worker, KPIs ---
builder.Services.Configure<PolicyOptions>(builder.Configuration.GetSection(PolicyOptions.Section));
builder.Services.AddHttpClient<AiClient>(client =>
{
    var baseUrl = (builder.Configuration["Ai:BaseUrl"] ?? "http://localhost:8000").TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<ClaimQueue>();
builder.Services.AddScoped<ClaimProcessor>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHostedService<ClaimWorker>();

// --- Optional CORS (only needed when the web app is hosted on a different origin than the API) ---
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
}

// --- API ---
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Create the schema (or apply migrations) and seed demo data in Development.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // No migrations in the project yet: create the schema directly (fine for the demo).
    // Once you add migrations (dotnet ef migrations add ...), they are applied instead.
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    // Demo users are only created in Development, and only when demo mode is on or you chose a seed password.
    var demoMode = app.Configuration.GetValue<bool>("Seed:DemoData");
    var hasSeedPassword = !string.IsNullOrWhiteSpace(app.Configuration["Seed:Password"]);
    if (app.Environment.IsDevelopment() && (demoMode || hasSeedPassword))
    {
        await DbSeeder.SeedAsync(db, app.Configuration);
        if (demoMode)
            await DemoDataSeeder.SeedAsync(db, app.Configuration);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive docs at /scalar
}

if (corsOrigins.Length > 0) app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Lets the web app show one-click demo logins, only when demo mode is on.
app.MapGet("/api/config", (IConfiguration cfg, IWebHostEnvironment env) =>
{
    var demo = env.IsDevelopment() && cfg.GetValue<bool>("Seed:DemoData");
    object[] accounts = demo
        ? new object[]
        {
            new { role = "Customer", email = "customer@returnflow.local" },
            new { role = "Agent", email = "agent@returnflow.local" },
            new { role = "Manager", email = "manager@returnflow.local" }
        }
        : Array.Empty<object>();

    return Results.Ok(new
    {
        demoMode = demo,
        demoPassword = demo ? (cfg["Seed:Password"] ?? "ChangeMe123!") : null,
        demoAccounts = accounts
    });
});

app.MapControllers();

app.Run();

// Needed so integration tests can reference the entry point.
public partial class Program;
