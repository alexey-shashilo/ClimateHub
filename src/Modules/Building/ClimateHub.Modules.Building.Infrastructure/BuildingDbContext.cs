using ClimateHub.Modules.Building.Domain.Aggregates;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Building.Infrastructure;

public class BuildingDbContext(DbContextOptions<BuildingDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Aggregates.Building> Buildings => Set<Domain.Aggregates.Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("building");

        modelBuilder.Entity<Domain.Aggregates.Building>(entity =>
        {
            entity.ToTable("buildings");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id)
                .HasConversion(v => v.Value, v => BuildingId.From(v))
                .ValueGeneratedNever();
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Address).HasMaxLength(500);
            entity.Property(b => b.CreatedAt).IsRequired();
            entity.Property(b => b.UpdatedAt).IsRequired();
            entity.Property(b => b.ConcurrencyToken).IsRequired();
            entity.Ignore(b => b.DomainEvents);

            // xmin-based optimistic concurrency
            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .IsRowVersion();

            entity.HasMany(b => b.Floors)
                .WithOne()
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Уникальный индекс на Name временно отключён (MVP)
        });

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("floors");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id)
                .HasConversion(v => v.Value, v => FloorId.From(v))
                .ValueGeneratedNever();
            entity.Property(f => f.BuildingId)
                .HasConversion(v => v.Value, v => BuildingId.From(v));
            entity.Property(f => f.Name).IsRequired().HasMaxLength(200);
            entity.Property(f => f.Level).IsRequired();
            entity.Property(f => f.CreatedAt).IsRequired();
            entity.Property(f => f.UpdatedAt).IsRequired();
            entity.Ignore(f => f.DomainEvents);

            entity.HasMany(f => f.Rooms)
                .WithOne()
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => new { f.BuildingId, f.Level }).IsUnique();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id)
                .HasConversion(v => v.Value, v => RoomId.From(v))
                .ValueGeneratedNever();
            entity.Property(r => r.FloorId)
                .HasConversion(v => v.Value, v => FloorId.From(v));
            entity.Property(r => r.BuildingId)
                .HasConversion(v => v.Value, v => BuildingId.From(v));
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Purpose).HasMaxLength(200);
            entity.Property(r => r.CreatedAt).IsRequired();
            entity.Property(r => r.UpdatedAt).IsRequired();
            entity.Ignore(r => r.DomainEvents);

            entity.HasIndex(r => new { r.FloorId, r.Name }).IsUnique();
        });
    }
}
