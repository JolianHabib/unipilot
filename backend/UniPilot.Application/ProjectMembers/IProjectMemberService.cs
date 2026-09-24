using UniPilot.Domain.Projects;

namespace UniPilot.Application.ProjectMembers;

public interface IProjectMemberService
{
    Task<IReadOnlyList<ProjectMemberResult>?> GetByProjectAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);

    Task<ProjectMemberOperationResult> AddAsync(
        Guid ownerId,
        Guid academicProjectId,
        string email,
        ProjectMemberRole role,
        CancellationToken cancellationToken = default);

    Task<ProjectMemberOperationResult> UpdateRoleAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        ProjectMemberRole role,
        CancellationToken cancellationToken = default);

    Task<ProjectMemberOperationStatus> RemoveAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<ProjectInvitationAcceptanceResult> AcceptInvitationAsync(
        Guid userId,
        string invitationToken,
        CancellationToken cancellationToken = default);
}
