using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Domain;

namespace ReturnFlow.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ReturnRequest> Returns => Set<ReturnRequest>();
    public DbSet<ReturnClaim> Claims => Set<ReturnClaim>();
    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<ReturnRequest>(e =>
        {
            e.Property(r => r.OrderReference).HasMaxLength(50);
            e.Property(r => r.Reason).HasMaxLength(500);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.Customer).WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ReturnClaim>(e =>
        {
            e.Property(c => c.Description).HasMaxLength(1000);
            e.Property(c => c.AmountRequested).HasPrecision(12, 2);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Processing).HasConversion<string>().HasMaxLength(30);
            e.Property(c => c.RoutingReason).HasMaxLength(300);
            e.Property(c => c.AiCategory).HasMaxLength(40);
            e.Property(c => c.AiReasons).HasMaxLength(1000);
            e.Property(c => c.AiModelVersion).HasMaxLength(40);
            e.HasOne(c => c.ReturnRequest).WithMany(r => r.Claims)
                .HasForeignKey(c => c.ReturnRequestId);
            e.HasMany(c => c.Decisions).WithOne(d => d.Claim)
                .HasForeignKey(d => d.ClaimId);
            e.HasIndex(c => c.Processing);
            e.HasIndex(c => c.CreatedAt);
        });

        b.Entity<Decision>(e =>
        {
            e.Property(d => d.Outcome).HasConversion<string>().HasMaxLength(20);
            e.Property(d => d.DecidedBy).HasConversion<string>().HasMaxLength(10);
            e.Property(d => d.Comment).HasMaxLength(500);
            e.HasOne(d => d.Agent).WithMany()
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.DecidedAt);
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(a => a.Action).HasMaxLength(100);
            e.Property(a => a.EntityType).HasMaxLength(50);
            e.HasIndex(a => a.Timestamp);
        });
    }
}
