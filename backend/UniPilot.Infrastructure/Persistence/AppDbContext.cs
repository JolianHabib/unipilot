using Microsoft.EntityFrameworkCore;
using UniPilot.Domain.Entities;

namespace UniPilot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.HasKey(x => x.Id);

        user.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(100);

        user.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        user.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        user.Property(x => x.CreatedAtUtc)
            .IsRequired();

        user.HasIndex(x => x.Email)
            .IsUnique();
    }
}