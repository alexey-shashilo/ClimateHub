using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Infrastructure.InternalEvents;

public class InternalEventsDbContext(DbContextOptions<InternalEventsDbContext> options) : DbContext(options)
{
    public DbSet<InternalEventOutbox> InternalEventOutbox => Set<InternalEventOutbox>();
    public DbSet<InternalEventInbox> InternalEventInbox => Set<InternalEventInbox>();
    public DbSet<SseEventLog> SseEventLog => Set<SseEventLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("platform");

        mb.Entity<InternalEventOutbox>(e =>
        {
            e.ToTable("internal_event_outbox");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.EventId).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            e.Property(x => x.AggregateType).HasMaxLength(50).IsRequired();
            e.Property(x => x.AggregateId).HasMaxLength(100);
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.Property(x => x.Headers).HasColumnType("jsonb");
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.Property(x => x.CausationId).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.LastFailureCode).HasMaxLength(100);

            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => new { x.Status, x.AvailableAt });
            e.HasIndex(x => new { x.Status, x.ProcessingStartedAt });
            e.HasIndex(x => new { x.EventType, x.CreatedAt });
            e.HasIndex(x => new { x.AggregateId, x.CreatedAt });
            e.HasIndex(x => x.RoomId);
        });

        mb.Entity<InternalEventInbox>(e =>
        {
            e.ToTable("internal_event_inbox");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ConsumerName).HasMaxLength(100).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.FailureCode).HasMaxLength(100);
            e.Property(x => x.PayloadHash).HasMaxLength(64);

            e.HasIndex(x => new { x.ConsumerName, x.EventId }).IsUnique();
            e.HasIndex(x => new { x.Status, x.ReceivedAt });
        });

        mb.Entity<SseEventLog>(e =>
        {
            e.ToTable("sse_event_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.EventId).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Payload).HasColumnType("jsonb");

            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => new { x.BuildingId, x.OccurredAt });
            e.HasIndex(x => new { x.RoomId, x.OccurredAt });
        });
    }
}
