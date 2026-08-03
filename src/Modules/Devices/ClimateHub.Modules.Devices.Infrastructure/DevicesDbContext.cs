using ClimateHub.Modules.Devices.Domain.Aggregates;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Devices.Infrastructure;

public class DevicesDbContext(DbContextOptions<DevicesDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Aggregates.Device> Devices => Set<Domain.Aggregates.Device>();
    public DbSet<Domain.Aggregates.DeviceCapability> Capabilities => Set<Domain.Aggregates.DeviceCapability>();
    public DbSet<Domain.Aggregates.DeviceAssignment> Assignments => Set<Domain.Aggregates.DeviceAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("device");

        modelBuilder.Entity<Domain.Aggregates.Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id)
                .HasConversion(v => v.Value, v => DeviceId.From(v))
                .ValueGeneratedNever();
            entity.Property(d => d.HardwareId).IsRequired().HasMaxLength(200);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.Property(d => d.ProtocolVersion).IsRequired().HasMaxLength(20);
            entity.Property(d => d.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(d => d.ConnectivityState)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(d => d.RegisteredAt).IsRequired();
            entity.Property(d => d.CreatedAt).IsRequired();
            entity.Property(d => d.UpdatedAt).IsRequired();
            entity.Property(d => d.CurrentBootId);
            entity.Property(d => d.LastSequenceNumber);
            entity.Property(d => d.LastSeenAt);
            entity.Ignore(d => d.DomainEvents);
            entity.Ignore(d => d.ActiveAssignment);
            entity.Ignore(d => d.AssignmentHistoryReadOnly);

            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .IsRowVersion();

            entity.Navigation(d => d.AssignmentHistory)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(d => d.AssignmentHistoryReadOnly);

            entity.OwnsOne(d => d.Model, m =>
            {
                m.Property(m => m.Manufacturer).HasColumnName("manufacturer").IsRequired().HasMaxLength(200);
                m.Property(m => m.ModelName).HasColumnName("model_name").IsRequired().HasMaxLength(200);
                m.Property(m => m.HardwareVersion).HasColumnName("hardware_version").HasMaxLength(100);
            });

            entity.HasIndex(d => d.HardwareId).IsUnique();
        });

        modelBuilder.Entity<Domain.Aggregates.DeviceCapability>(entity =>
        {
            entity.ToTable("capabilities");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(100);
            entity.Property(c => c.DataType).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Unit).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(20);
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.DeviceId)
                .HasConversion(v => v.Value, v => DeviceId.From(v));

            entity.HasIndex(c => new { c.DeviceId, c.Code }).IsUnique();
        });

        modelBuilder.Entity<Domain.Aggregates.DeviceAssignment>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedOnAdd();
            entity.Property(a => a.DeviceId)
                .HasConversion(v => v.Value, v => DeviceId.From(v));
            entity.Property(a => a.RoomId)
                .HasConversion(v => v.Value, v => RoomId.From(v));
            entity.Property(a => a.AssignedAt).IsRequired();
            entity.Property(a => a.CompletedAt);
            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Ignore(a => a.DomainEvents);

            entity.HasIndex(a => new { a.DeviceId, a.Status }).HasFilter("\"Status\" = 'Active'");

            entity.HasOne<Domain.Aggregates.Device>()
                .WithMany(d => d.AssignmentHistory)
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}