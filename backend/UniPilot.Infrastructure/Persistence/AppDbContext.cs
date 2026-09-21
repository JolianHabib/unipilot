using Microsoft.EntityFrameworkCore;
using UniPilot.Domain.Entities;

namespace UniPilot.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users =>
        Set<User>();

    public DbSet<Course> Courses =>
        Set<Course>();

    public DbSet<AcademicProject> AcademicProjects =>
        Set<AcademicProject>();

    public DbSet<ProjectDocument> ProjectDocuments =>
        Set<ProjectDocument>();

    public DbSet<DocumentPage> DocumentPages =>
        Set<DocumentPage>();

    public DbSet<ProjectRequirement> ProjectRequirements =>
        Set<ProjectRequirement>();

    public DbSet<ProjectTask> ProjectTasks =>
        Set<ProjectTask>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

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
user.Property(x => x.PasswordHash)
    .IsRequired()
    .HasMaxLength(255);

user.Property(x =>
        x.PasswordResetTokenHash)
    .HasMaxLength(64);

user.Property(x =>
    x.PasswordResetTokenExpiresAtUtc);

user.Property(x => x.CreatedAtUtc)
    .IsRequired();
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

        var projectDocument =
            modelBuilder.Entity<ProjectDocument>();

        projectDocument.ToTable("project_documents");

        projectDocument.HasKey(x => x.Id);

        projectDocument.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        projectDocument.Property(x => x.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        projectDocument.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        projectDocument.Property(x => x.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        projectDocument.Property(x => x.DocumentType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(40);

        projectDocument.Property(x => x.ProcessingStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        projectDocument.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        projectDocument.Property(x => x.UploadedAtUtc)
            .IsRequired();

        projectDocument.HasIndex(x =>
            x.AcademicProjectId);

        projectDocument.HasIndex(x => new
            {
                x.AcademicProjectId,
                x.ContentHash
            })
            .IsUnique();

        projectDocument.HasOne(x =>
                x.AcademicProject)
            .WithMany(x =>
                x.Documents)
            .HasForeignKey(x =>
                x.AcademicProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        var documentPage =
            modelBuilder.Entity<DocumentPage>();

        documentPage.ToTable("document_pages");

        documentPage.HasKey(x => x.Id);

        documentPage.Property(x => x.PageNumber)
            .IsRequired();

        documentPage.Property(x => x.Text)
            .IsRequired()
            .HasColumnType("text");

        documentPage.HasIndex(x =>
            x.ProjectDocumentId);

        documentPage.HasIndex(x => new
            {
                x.ProjectDocumentId,
                x.PageNumber
            })
            .IsUnique();

        documentPage.HasOne(x =>
                x.ProjectDocument)
            .WithMany(x =>
                x.Pages)
            .HasForeignKey(x =>
                x.ProjectDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        var projectRequirement =
            modelBuilder.Entity<ProjectRequirement>();

        projectRequirement.ToTable(
            "project_requirements");

        projectRequirement.HasKey(x => x.Id);

        projectRequirement.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(250);

        projectRequirement.Property(x => x.Description)
            .IsRequired()
            .HasColumnType("text");

        projectRequirement.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(40);

        projectRequirement.Property(x => x.Priority)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        projectRequirement.Property(x => x.IsCompleted)
            .IsRequired();

        projectRequirement.Property(x => x.CreatedAtUtc)
            .IsRequired();

        projectRequirement.HasIndex(x =>
            x.AcademicProjectId);

        projectRequirement.HasIndex(x =>
            x.ProjectDocumentId);

        projectRequirement.HasOne(x =>
                x.AcademicProject)
            .WithMany(x =>
                x.Requirements)
            .HasForeignKey(x =>
                x.AcademicProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        projectRequirement.HasOne(x =>
                x.ProjectDocument)
            .WithMany(x =>
                x.Requirements)
            .HasForeignKey(x =>
                x.ProjectDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        var projectTask =
            modelBuilder.Entity<ProjectTask>();

        projectTask.ToTable("project_tasks");

        projectTask.HasKey(x => x.Id);

        projectTask.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(250);

        projectTask.Property(x => x.Description)
            .HasColumnType("text");

        projectTask.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        projectTask.Property(x => x.Priority)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        projectTask.Property(x => x.Position)
            .IsRequired();

        projectTask.Property(x => x.CreatedAtUtc)
            .IsRequired();

        projectTask.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        projectTask.HasIndex(x =>
            x.AcademicProjectId);

        projectTask.HasIndex(x =>
            x.ProjectRequirementId);

        projectTask.HasIndex(x => new
        {
            x.AcademicProjectId,
            x.Status,
            x.Position
        });

        projectTask.HasOne(x =>
                x.AcademicProject)
            .WithMany(x =>
                x.Tasks)
            .HasForeignKey(x =>
                x.AcademicProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        projectTask.HasOne(x =>
                x.ProjectRequirement)
            .WithMany(x =>
                x.Tasks)
            .HasForeignKey(x =>
                x.ProjectRequirementId)
            .OnDelete(DeleteBehavior.SetNull);

        var notification =
    modelBuilder.Entity<Notification>();

notification.ToTable("notifications");

notification.HasKey(x => x.Id);

notification.Property(x => x.Type)
    .HasConversion<string>()
    .IsRequired()
    .HasMaxLength(30);

notification.Property(x => x.Title)
    .IsRequired()
    .HasMaxLength(150);

notification.Property(x => x.Message)
    .IsRequired()
    .HasMaxLength(1000);

notification.Property(x => x.ActionUrl)
    .HasMaxLength(500);
notification.Property(x =>
        x.DeduplicationKey)
    .HasMaxLength(200);

notification.HasIndex(x => new
{
    x.UserId,
    x.DeduplicationKey
})
.IsUnique();
notification.Property(x => x.IsRead)
    .IsRequired();

notification.Property(x => x.CreatedAtUtc)
    .IsRequired();

notification.HasIndex(x => new
{
    x.UserId,
    x.CreatedAtUtc
});

notification.HasOne(x => x.User)
    .WithMany()
    .HasForeignKey(x => x.UserId)
    .OnDelete(DeleteBehavior.Cascade);
    }
}