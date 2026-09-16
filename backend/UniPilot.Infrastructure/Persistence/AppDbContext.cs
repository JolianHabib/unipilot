using Microsoft.EntityFrameworkCore;
using UniPilot.Domain.Entities;

namespace UniPilot.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<AcademicProject> AcademicProjects =>
        Set<AcademicProject>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
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

        var course = modelBuilder.Entity<Course>();

        course.ToTable("courses");

        course.HasKey(x => x.Id);

        course.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        course.Property(x => x.Code)
            .HasMaxLength(50);

        course.Property(x => x.Description)
            .HasMaxLength(1000);

        course.Property(x => x.CreatedAtUtc)
            .IsRequired();

        course.HasIndex(x => x.OwnerId);

        course.HasOne(x => x.Owner)
            .WithMany(x => x.Courses)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        var academicProject =
            modelBuilder.Entity<AcademicProject>();

        academicProject.ToTable("academic_projects");

        academicProject.HasKey(x => x.Id);

        academicProject.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        academicProject.Property(x => x.Description)
            .HasMaxLength(2000);

        academicProject.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        academicProject.Property(x => x.CreatedAtUtc)
            .IsRequired();

        academicProject.HasIndex(x => x.CourseId);

        academicProject.HasOne(x => x.Course)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}