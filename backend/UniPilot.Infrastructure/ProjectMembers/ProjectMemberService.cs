using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Activities;
using UniPilot.Application.ProjectMembers;
using UniPilot.Domain.Activities;
using UniPilot.Domain.Entities;
using UniPilot.Domain.Projects;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.ProjectMembers;

public sealed class ProjectMemberService(
    AppDbContext dbContext,
    IProjectActivityService activityService)
    : IProjectMemberService
{
    public async Task<IReadOnlyList<ProjectMemberResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(
                ownerId,
                academicProjectId,
                cancellationToken))
        {
            return null;
        }

        return await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member =>
                member.AcademicProjectId == academicProjectId)
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new ProjectMemberResult(
                member.Id,
                member.AcademicProjectId,
                member.UserId,
                member.User.FullName,
                member.User.Email,
                member.Role.ToString(),
                member.JoinedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectMemberOperationResult> AddAsync(
        Guid ownerId,
        Guid academicProjectId,
        string email,
        ProjectMemberRole role,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(
                ownerId,
                academicProjectId,
                cancellationToken))
        {
            return Result(ProjectMemberOperationStatus.ProjectNotFound);
        }

        var normalizedEmail = email.Trim().ToLower();

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            return Result(ProjectMemberOperationStatus.UserNotFound);
        }

        if (user.Id == ownerId)
        {
            return Result(ProjectMemberOperationStatus.OwnerCannotBeMember);
        }

        var alreadyMember = await dbContext.ProjectMembers.AnyAsync(
            member =>
                member.AcademicProjectId == academicProjectId &&
                member.UserId == user.Id,
            cancellationToken);

        if (alreadyMember)
        {
            return Result(ProjectMemberOperationStatus.AlreadyMember);
        }

        var member = new ProjectMember
        {
            AcademicProjectId = academicProjectId,
            UserId = user.Id,
            User = user,
            Role = role,
            JoinedAtUtc = DateTime.UtcNow
        };

        dbContext.ProjectMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.ProjectMemberAdded,
            "Project member added",
            $"{user.FullName} was added as {role}.",
            cancellationToken);

        return Result(
            ProjectMemberOperationStatus.Success,
            Map(member));
    }

    public async Task<ProjectMemberOperationResult> UpdateRoleAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        ProjectMemberRole role,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(
                ownerId,
                academicProjectId,
                cancellationToken))
        {
            return Result(ProjectMemberOperationStatus.ProjectNotFound);
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == memberId &&
                    item.AcademicProjectId == academicProjectId,
                cancellationToken);

        if (member is null)
        {
            return Result(ProjectMemberOperationStatus.MemberNotFound);
        }

        member.Role = role;
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.ProjectMemberRoleChanged,
            "Project member role changed",
            $"{member.User.FullName} is now {role}.",
            cancellationToken);

        return Result(
            ProjectMemberOperationStatus.Success,
            Map(member));
    }

    public async Task<ProjectMemberOperationStatus> RemoveAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(
                ownerId,
                academicProjectId,
                cancellationToken))
        {
            return ProjectMemberOperationStatus.ProjectNotFound;
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == memberId &&
                    item.AcademicProjectId == academicProjectId,
                cancellationToken);

        if (member is null)
        {
            return ProjectMemberOperationStatus.MemberNotFound;
        }

        var memberName = member.User.FullName;

        dbContext.ProjectMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.ProjectMemberRemoved,
            "Project member removed",
            $"{memberName} was removed from the project.",
            cancellationToken);

        return ProjectMemberOperationStatus.Success;
    }

    private Task<bool> OwnsProjectAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken)
    {
        return dbContext.AcademicProjects.AnyAsync(
            project =>
                project.Id == academicProjectId &&
                project.Course.OwnerId == ownerId,
            cancellationToken);
    }

    private static ProjectMemberResult Map(ProjectMember member)
    {
        return new ProjectMemberResult(
            member.Id,
            member.AcademicProjectId,
            member.UserId,
            member.User.FullName,
            member.User.Email,
            member.Role.ToString(),
            member.JoinedAtUtc);
    }

    private static ProjectMemberOperationResult Result(
        ProjectMemberOperationStatus status,
        ProjectMemberResult? member = null)
    {
        return new ProjectMemberOperationResult(status, member);
    }
}
