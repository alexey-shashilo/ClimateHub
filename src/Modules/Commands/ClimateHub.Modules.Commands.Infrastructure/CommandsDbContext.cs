using ClimateHub.Modules.Commands.Domain;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Commands.Infrastructure;

public class CommandsDbContext(DbContextOptions<CommandsDbContext> options) : DbContext(options)
{
    public DbSet<Command> Commands => Set<Command>();
    public DbSet<CommandAttempt> Attempts => Set<CommandAttempt>();
    public DbSet<CommandOutbox> Outbox => Set<CommandOutbox>();
    public DbSet<CommandInboxMessage> Inbox => Set<CommandInboxMessage>();
    public DbSet<DeviceCapabilityState> CapabilityStates => Set<DeviceCapabilityState>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("command");

        mb.Entity<Command>(e =>
        {
            e.ToTable("commands");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasConversion(v => v.Value, v => CommandId.From(v)).ValueGeneratedNever();
            e.Property(c => c.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(c => c.RoomId).HasConversion(v => v!.Value.Value, v => RoomId.From(v));
            e.Property(c => c.DeviceId).HasConversion(v => v.Value, v => DeviceId.From(v));
            e.Property(c => c.CapabilityCode).IsRequired().HasMaxLength(100);
            e.Property(c => c.Operation).IsRequired().HasMaxLength(50);
            e.Property(c => c.ParametersJson).IsRequired();
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(c => c.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.CreatedBy).HasMaxLength(100);
            e.Property(c => c.CorrelationId).HasMaxLength(100);
            e.Property(c => c.LastErrorCode).HasMaxLength(100);
            e.Property(c => c.LastErrorMessage).HasMaxLength(500);
            e.Property(c => c.Version).IsRequired().IsConcurrencyToken();
            e.Ignore(c => c.DomainEvents);
            e.HasIndex(c => new { c.DeviceId, c.CreatedAt });
            e.HasIndex(c => new { c.RoomId, c.CreatedAt });
            e.HasIndex(c => new { c.Status, c.ExpiresAt });
        });

        mb.Entity<CommandAttempt>(e =>
        {
            e.ToTable("command_attempts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedOnAdd();
            e.Property(a => a.CommandId).HasConversion(v => v.Value, v => CommandId.From(v));
            e.Property(a => a.MqttMessageId).HasMaxLength(100);
            e.Property(a => a.FailureCode).HasMaxLength(100);
        });

        mb.Entity<CommandOutbox>(e =>
        {
            e.ToTable("command_outbox");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).ValueGeneratedOnAdd();
            e.Property(o => o.CommandId).HasConversion(v => v.Value, v => CommandId.From(v));
            e.Property(o => o.DeviceId).HasConversion(v => v.Value, v => DeviceId.From(v));
            e.Property(o => o.Status).IsRequired().HasMaxLength(30);
            e.Property(o => o.LastFailureCode).HasMaxLength(100);
            e.HasIndex(o => o.Status);
        });

        mb.Entity<CommandInboxMessage>(e =>
        {
            e.ToTable("command_inbox");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedOnAdd();
            e.Property(i => i.Source).IsRequired().HasMaxLength(50);
            e.Property(i => i.MessageId).IsRequired().HasMaxLength(100);
            e.Property(i => i.CommandId).HasConversion(v => v.Value, v => CommandId.From(v));
            e.Property(i => i.MessageType).IsRequired().HasMaxLength(50);
            e.Property(i => i.Status).IsRequired().HasMaxLength(30);
            e.Property(i => i.FailureCode).HasMaxLength(100);
            e.HasIndex(i => new { i.Source, i.MessageId }).IsUnique();
        });

        mb.Entity<DeviceCapabilityState>(e =>
        {
            e.ToTable("device_capability_states");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedOnAdd();
            e.Property(s => s.DeviceId).HasConversion(v => v.Value, v => DeviceId.From(v));
            e.Property(s => s.CapabilityCode).IsRequired().HasMaxLength(100);
            e.Property(s => s.DesiredValue);
            e.Property(s => s.DesiredByCommandId)
                .HasConversion(v => v!.Value.Value, v => new CommandId(v));
            e.Property(s => s.ReportedByCommandId)
                .HasConversion(v => v!.Value.Value, v => new CommandId(v));
            e.Property(s => s.ReportedValue);
            e.Property(s => s.Quality).IsRequired().HasMaxLength(30);
            e.Property(s => s.Version).IsRequired();
            e.HasIndex(s => new { s.DeviceId, s.CapabilityCode }).IsUnique();
        });
    }
}