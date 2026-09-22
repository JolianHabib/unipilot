using UniPilot.Domain.Activities;

namespace UniPilot.Application.Activities;

public interface IProjectActivityService
{
    Task<IReadOnlyList<ProjectActivityResult>?> GetByProjectAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        Guid userId,
        Guid academicProjectId,
        ProjectActivityType type,
        string title,
        string? description = null,
        CancellationToken cancellationToken = default);
}
