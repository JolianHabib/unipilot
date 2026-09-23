using UniPilot.Domain.Projects;

namespace UniPilot.Application.ProjectAccess;

public interface IProjectAccessService
{
    Task<ProjectAccessLevel> GetAccessLevelAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);

    Task<bool> CanViewAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);

    Task<bool> CanEditAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);

    Task<bool> IsOwnerAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);
}
