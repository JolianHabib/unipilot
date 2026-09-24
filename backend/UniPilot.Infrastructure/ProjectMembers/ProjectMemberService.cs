using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UniPilot.Application.Activities;
using UniPilot.Application.ProjectMembers;
using UniPilot.Domain.Activities;
using UniPilot.Domain.Entities;
using UniPilot.Domain.Projects;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.ProjectMembers;

public sealed class ProjectMemberService(
    AppDbContext dbContext,
    IProjectActivityService activityService,
    IProjectInvitationEmailSender invitationEmailSender,
    IConfiguration configuration)
    : IProjectMemberService
{
    private static readonly TimeSpan InvitationLifetime =
        TimeSpan.FromDays(7);

    public async Task<IReadOnlyList<ProjectMemberResult>?> GetByProjectAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(ownerId, academicProjectId, cancellationToken))
        {
            return null;
        }

        return await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.AcademicProjectId == academicProjectId)
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new ProjectMemberResult(
                member.Id,
                member.AcademicProjectId,
                member.UserId,
                member.User == null ? "Pending invitation" : member.User.FullName,
                member.User == null ? member.InvitedEmail! : member.User.Email,
                member.Role.ToString(),
                member.JoinedAtUtc,
                member.UserId == null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectMemberOperationResult> AddAsync(
        Guid ownerId,
        Guid academicProjectId,
        string email,
        ProjectMemberRole role,
        CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedProjectAsync(
            ownerId,
            academicProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result(ProjectMemberOperationStatus.ProjectNotFound);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (user?.Id == ownerId)
        {
            return Result(ProjectMemberOperationStatus.OwnerCannotBeMember);
        }

        var alreadyMember = await dbContext.ProjectMembers.AnyAsync(
            member =>
                member.AcademicProjectId == academicProjectId &&
                (member.InvitedEmail == normalizedEmail ||
                    (user != null && member.UserId == user.Id)),
            cancellationToken);

        if (alreadyMember)
        {
            return Result(ProjectMemberOperationStatus.AlreadyMember);
        }

        var now = DateTime.UtcNow;
        var invitationToken = user is null ? CreateInvitationToken() : null;
        var member = new ProjectMember
        {
            AcademicProjectId = academicProjectId,
            UserId = user?.Id,
            User = user,
            InvitedEmail = normalizedEmail,
            InvitationTokenHash = invitationToken is null
                ? null
                : HashInvitationToken(invitationToken),
            InvitationExpiresAtUtc = invitationToken is null
                ? null
                : now.Add(InvitationLifetime),
            InvitationSentAtUtc = invitationToken is null ? null : now,
            Role = role,
            JoinedAtUtc = now
        };

        dbContext.ProjectMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (invitationToken is not null)
        {
            try
            {
                await SendInvitationAsync(
                    member,
                    project.OwnerName,
                    project.Title,
                    invitationToken,
                    cancellationToken);
            }
            catch
            {
                dbContext.ProjectMembers.Remove(member);
                await dbContext.SaveChangesAsync(CancellationToken.None);
                throw;
            }
        }

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.ProjectMemberAdded,
            user is null ? "Project invitation sent" : "Project member added",
            user is null
                ? $"{normalizedEmail} was invited as {role}."
                : $"{user.FullName} was added as {role}.",
            cancellationToken);

        return Result(ProjectMemberOperationStatus.Success, Map(member));
    }

    public async Task<ProjectMemberOperationResult> ResendInvitationAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedProjectAsync(
            ownerId,
            academicProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result(ProjectMemberOperationStatus.ProjectNotFound);
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.Id == memberId &&
                    item.AcademicProjectId == academicProjectId,
                cancellationToken);

        if (member is null)
        {
            return Result(ProjectMemberOperationStatus.MemberNotFound);
        }

        if (member.UserId is not null ||
            string.IsNullOrWhiteSpace(member.InvitedEmail))
        {
            return Result(ProjectMemberOperationStatus.InvitationNotPending);
        }

        var previousHash = member.InvitationTokenHash;
        var previousExpiry = member.InvitationExpiresAtUtc;
        var previousSentAt = member.InvitationSentAtUtc;
        var invitationToken = CreateInvitationToken();
        var now = DateTime.UtcNow;

        member.InvitationTokenHash = HashInvitationToken(invitationToken);
        member.InvitationExpiresAtUtc = now.Add(InvitationLifetime);
        member.InvitationSentAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await SendInvitationAsync(
                member,
                project.OwnerName,
                project.Title,
                invitationToken,
                cancellationToken);
        }
        catch
        {
            member.InvitationTokenHash = previousHash;
            member.InvitationExpiresAtUtc = previousExpiry;
            member.InvitationSentAtUtc = previousSentAt;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.ProjectMemberAdded,
            "Project invitation resent",
            $"The invitation for {member.InvitedEmail} was sent again.",
            cancellationToken);

        return Result(ProjectMemberOperationStatus.Success, Map(member));
    }

    public async Task<ProjectMemberOperationResult> UpdateRoleAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        ProjectMemberRole role,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(ownerId, academicProjectId, cancellationToken))
        {
            return Result(ProjectMemberOperationStatus.ProjectNotFound);
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.Id == memberId &&
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
            member.User is null
                ? $"The invitation for {member.InvitedEmail} is now {role}."
                : $"{member.User.FullName} is now {role}.",
            cancellationToken);

        return Result(ProjectMemberOperationStatus.Success, Map(member));
    }

    public async Task<ProjectMemberOperationStatus> RemoveAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(ownerId, academicProjectId, cancellationToken))
        {
            return ProjectMemberOperationStatus.ProjectNotFound;
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.Id == memberId &&
                    item.AcademicProjectId == academicProjectId,
                cancellationToken);

        if (member is null)
        {
            return ProjectMemberOperationStatus.MemberNotFound;
        }

        var memberName = member.User?.FullName
            ?? member.InvitedEmail
            ?? "Pending member";

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

    public async Task<ProjectInvitationAcceptanceResult> AcceptInvitationAsync(
        Guid userId,
        string invitationToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashInvitationToken(invitationToken.Trim());
        var member = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            item => item.InvitationTokenHash == tokenHash,
            cancellationToken);

        if (member is null)
        {
            return AcceptanceResult(ProjectInvitationAcceptanceStatus.InvalidToken);
        }

        if (member.InvitationExpiresAtUtc is null ||
            member.InvitationExpiresAtUtc <= DateTime.UtcNow)
        {
            return AcceptanceResult(ProjectInvitationAcceptanceStatus.Expired);
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);

        if (user is null ||
            !string.Equals(
                user.Email,
                member.InvitedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            return AcceptanceResult(ProjectInvitationAcceptanceStatus.EmailMismatch);
        }

        var existingMembership = await dbContext.ProjectMembers
            .SingleOrDefaultAsync(
                item => item.Id != member.Id &&
                    item.AcademicProjectId == member.AcademicProjectId &&
                    item.UserId == userId,
                cancellationToken);

        if (existingMembership is not null)
        {
            dbContext.ProjectMembers.Remove(member);
            await dbContext.SaveChangesAsync(cancellationToken);
            return AcceptanceResult(
                ProjectInvitationAcceptanceStatus.Success,
                existingMembership.AcademicProjectId);
        }

        member.UserId = user.Id;
        member.User = user;
        member.InvitedEmail = null;
        member.InvitationTokenHash = null;
        member.InvitationExpiresAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            user.Id,
            member.AcademicProjectId,
            ProjectActivityType.ProjectMemberAdded,
            "Project invitation accepted",
            $"{user.FullName} accepted the project invitation.",
            cancellationToken);

        return AcceptanceResult(
            ProjectInvitationAcceptanceStatus.Success,
            member.AcademicProjectId);
    }

    private async Task<OwnedProject?> GetOwnedProjectAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AcademicProjects
            .AsNoTracking()
            .Where(item => item.Id == academicProjectId &&
                item.Course.OwnerId == ownerId)
            .Select(item => new OwnedProject(
                item.Title,
                item.Course.Owner.FullName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Task<bool> OwnsProjectAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken)
    {
        return dbContext.AcademicProjects.AnyAsync(
            project => project.Id == academicProjectId &&
                project.Course.OwnerId == ownerId,
            cancellationToken);
    }

    private Task SendInvitationAsync(
        ProjectMember member,
        string ownerName,
        string projectTitle,
        string invitationToken,
        CancellationToken cancellationToken)
    {
        return invitationEmailSender.SendProjectInvitationAsync(
            member.InvitedEmail!,
            ownerName,
            projectTitle,
            member.Role.ToString(),
            BuildInvitationLink(invitationToken),
            cancellationToken);
    }

    private string BuildInvitationLink(string invitationToken)
    {
        var frontendBaseUrl = configuration["Frontend:BaseUrl"]
            ?? "http://localhost:5173";

        return $"{frontendBaseUrl.TrimEnd('/')}" +
            "/invitations/accept?token=" +
            Uri.EscapeDataString(invitationToken);
    }

    private static string CreateInvitationToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashInvitationToken(string invitationToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(invitationToken)));
    }

    private static ProjectMemberResult Map(ProjectMember member)
    {
        return new ProjectMemberResult(
            member.Id,
            member.AcademicProjectId,
            member.UserId,
            member.User?.FullName ?? "Pending invitation",
            member.User?.Email ?? member.InvitedEmail ?? string.Empty,
            member.Role.ToString(),
            member.JoinedAtUtc,
            member.UserId is null);
    }

    private static ProjectMemberOperationResult Result(
        ProjectMemberOperationStatus status,
        ProjectMemberResult? member = null)
    {
        return new ProjectMemberOperationResult(status, member);
    }

    private static ProjectInvitationAcceptanceResult AcceptanceResult(
        ProjectInvitationAcceptanceStatus status,
        Guid? academicProjectId = null)
    {
        return new ProjectInvitationAcceptanceResult(status, academicProjectId);
    }

    private sealed record OwnedProject(
        string Title,
        string OwnerName);
}
