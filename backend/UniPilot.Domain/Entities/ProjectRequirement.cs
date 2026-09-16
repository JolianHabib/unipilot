using UniPilot.Domain.Requirements;

namespace UniPilot.Domain.Entities;

public sealed class ProjectRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AcademicProjectId { get; set; }

    public AcademicProject AcademicProject { get; set; } = null!;

    public Guid? ProjectDocumentId { get; set; }

    public ProjectDocument? ProjectDocument { get; set; }

    public int? SourcePageNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RequirementType Type { get; set; }

    public RequirementPriority Priority { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}