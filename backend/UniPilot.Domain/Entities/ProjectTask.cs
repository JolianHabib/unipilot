using UniPilot.Domain.Tasks;

namespace UniPilot.Domain.Entities;

public sealed class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AcademicProjectId { get; set; }

    public AcademicProject AcademicProject { get; set; } = null!;

    public Guid? ProjectRequirementId { get; set; }

    public ProjectRequirement? ProjectRequirement { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectTaskStatus Status { get; set; } =
        ProjectTaskStatus.ToDo;

    public ProjectTaskPriority Priority { get; set; } =
        ProjectTaskPriority.Medium;

    public DateTime? DueDateUtc { get; set; }

    public int Position { get; set; }

    public DateTime CreatedAtUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } =
        DateTime.UtcNow;
}