using ClimateHub.Modules.Environment.Domain;
using ClimateHub.Modules.Environment.Infrastructure.Models;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Environment.Infrastructure;

public class EnvironmentDbContext(DbContextOptions<EnvironmentDbContext> options) : DbContext(options)
{
    public DbSet<RoomParameterEntity> RoomParameters => Set<RoomParameterEntity>();
    public DbSet<MessageInboxEntity> MessageInbox => Set<MessageInboxEntity>();
    public DbSet<TelemetryOutboxEntity> TelemetryOutbox => Set<TelemetryOutboxEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("environment");

        modelBuilder.Entity<RoomParameterEntity>(entity =>
        {
            entity.ToTable("room_parameters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoomId)
                .HasConversion(v => v.Value, v => RoomId.From(v));
            entity.Property(e => e.Parameter).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Quality).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SourceDeviceId)
                .HasConversion(v => v!.Value.Value, v => DeviceId.From(v));

            entity.HasIndex(e => new { e.RoomId, e.Parameter }).IsUnique();
        });

        modelBuilder.Entity<MessageInboxEntity>(entity =>
        {
            entity.ToTable("message_inbox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MessageIdStr).HasColumnName("message_id").IsRequired().HasMaxLength(64);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DeviceIdStr).HasColumnName("device_id").IsRequired().HasMaxLength(64);
            entity.Property(e => e.BootIdStr).HasColumnName("boot_id").HasMaxLength(64);
            entity.Property(e => e.SequenceNumber);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorCode).HasMaxLength(100);

            entity.HasIndex(e => new { e.Source, e.MessageIdStr }).IsUnique();
        });

        modelBuilder.Entity<TelemetryOutboxEntity>(entity =>
        {
            entity.ToTable("telemetry_outbox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoomId)
                .HasConversion(v => v.Value, v => RoomId.From(v));
            entity.Property(e => e.DeviceId)
                .HasConversion(v => v.Value, v => DeviceId.From(v));
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorCode).HasMaxLength(200);
            entity.Property(e => e.ProcessingStartedAt);

            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<RoomPolicy>(entity =>
        {
            entity.ToTable("room_policies");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id)
                .HasConversion(v => v.Value, v => RoomId.From(v))
                .ValueGeneratedNever();

            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        });
    }
}