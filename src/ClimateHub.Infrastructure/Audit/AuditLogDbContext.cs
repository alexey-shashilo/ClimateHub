using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClimateHub.Infrastructure.Audit;

public class AuditLogDbContext : DbContext
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(x => x.AuditEventId);
            e.Property(x => x.ActorType).HasMaxLength(64).IsRequired();
            e.Property(x => x.ActorId).HasMaxLength(256).IsRequired();
            e.Property(x => x.SessionId).HasMaxLength(256);
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.Action).HasMaxLength(128).IsRequired();
            e.Property(x => x.ResourceType).HasMaxLength(64);
            e.Property(x => x.ResourceId).HasMaxLength(256);
            e.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
            e.Property(x => x.ReasonCode).HasMaxLength(64);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.CorrelationId).HasMaxLength(128);
            e.Property(x => x.TraceId).HasMaxLength(128);

            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => x.ActorId);
            e.HasIndex(x => x.BuildingId);
            e.HasIndex(x => x.Action);
        });
    }
}

public class AppendOnlySaveChangesInterceptor : ISaveChangesInterceptor
{
    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var context = eventData.Context;
        if (context is null) return result;

        var entries = context.ChangeTracker.Entries<AuditEvent>();
        foreach (var entry in entries)
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Audit log is append-only; modifications and deletions are not allowed.");
        }
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        return ValueTask.FromResult(SavingChanges(eventData, result));
    }
}
