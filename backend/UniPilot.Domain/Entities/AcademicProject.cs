namespace UniPilot.Domain.Entities;

public sealed class AcademicProject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public AcademicProjectStatus Status { get; set; } =
        AcademicProjectStatus.Draft;

    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}