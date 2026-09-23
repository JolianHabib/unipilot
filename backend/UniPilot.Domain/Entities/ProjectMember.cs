using UniPilot.Domain.Projects;

namespace UniPilot.Domain.Entities;

public sealed class ProjectMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AcademicProjectId { get; set; }

    public AcademicProject AcademicProject { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } =
        ProjectMemberRole.Viewer;

    public DateTime JoinedAtUtc { get; set; } =
        DateTime.UtcNow;
}
