using Microsoft.EntityFrameworkCore;
using UniPilot.Application.ProjectAccess;
using UniPilot.Domain.Projects;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.ProjectAccess;

public sealed class ProjectAccessService(
    AppDbContext dbContext) : IProjectAccessService
{
    public async Task<ProjectAccessLevel> GetAccessLevelAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.AcademicProjects
            .AsNoTracking()
            .Where(item => item.Id == academicProjectId)
            .Select(item => new
            {
                IsOwner = item.Course.OwnerId == userId,
                MemberRole = item.Members
                    .Where(member => member.UserId == userId)
                    .Select(member =>
                        (ProjectMemberRole?)member.Role)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return ProjectAccessLevel.None;
        }

        if (project.IsOwner)
        {
            return ProjectAccessLevel.Owner;
        }

        return project.MemberRole switch
        {
            ProjectMemberRole.Editor => ProjectAccessLevel.Editor,
            ProjectMemberRole.Viewer => ProjectAccessLevel.Viewer,
            _ => ProjectAccessLevel.None
        };
    }

    public async Task<bool> CanViewAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        return await GetAccessLevelAsync(
            userId,
            academicProjectId,
            cancellationToken) >= ProjectAccessLevel.Viewer;
    }

    public async Task<bool> CanEditAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        return await GetAccessLevelAsync(
            userId,
            academicProjectId,
            cancellationToken) >= ProjectAccessLevel.Editor;
    }

    public async Task<bool> IsOwnerAsync(
        Guid userId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        return await GetAccessLevelAsync(
            userId,
            academicProjectId,
            cancellationToken) == ProjectAccessLevel.Owner;
    }
}
