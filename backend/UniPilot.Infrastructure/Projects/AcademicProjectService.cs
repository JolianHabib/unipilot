using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniPilot.Application.Activities;
using UniPilot.Application.Documents;
using UniPilot.Application.Projects;
using UniPilot.Domain.Activities;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Projects;

public sealed class AcademicProjectService(
    AppDbContext dbContext,
    IFileStorage fileStorage,
    IProjectActivityService activityService,
    ILogger<AcademicProjectService> logger)
    : IAcademicProjectService
{
    public async Task<AcademicProjectResult?> CreateAsync(
        CreateAcademicProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownsCourse = await dbContext.Courses.AnyAsync(
            course =>
                course.Id == command.CourseId &&
                course.OwnerId == command.OwnerId,
            cancellationToken);

        if (!ownsCourse)
        {
            return null;
        }

        var project = new AcademicProject
        {
            CourseId = command.CourseId,
            Title = command.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
            DueDateUtc = command.DueDateUtc?.ToUniversalTime(),
            Status = AcademicProjectStatus.Draft
        };

        dbContext.AcademicProjects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            command.OwnerId,
            project.Id,
            ProjectActivityType.ProjectCreated,
            "Project created",
            $"{project.Title} was created with Draft status.",
            cancellationToken);

        return Map(project, "Owner");
    }

    public async Task<IReadOnlyList<AcademicProjectResult>?>
        GetByCourseAsync(
            Guid ownerId,
            Guid courseId,
            CancellationToken cancellationToken = default)
    {
        var ownsCourse = await dbContext.Courses.AnyAsync(
            course =>
                course.Id == courseId &&
                course.OwnerId == ownerId,
            cancellationToken);

        if (!ownsCourse)
        {
            return null;
        }

        return await dbContext.AcademicProjects
            .AsNoTracking()
            .Where(project => project.CourseId == courseId)
            .OrderByDescending(project => project.CreatedAtUtc)
            .Select(project => new AcademicProjectResult(
                project.Id,
                project.CourseId,
                project.Title,
                project.Description,
                project.DueDateUtc,
                project.Status.ToString(),
                project.CreatedAtUtc,
                "Owner"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AcademicProjectResult>>
        GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.AcademicProjects
            .AsNoTracking()
            .Where(project =>
                project.Course.OwnerId == ownerId ||
                project.Members.Any(member =>
                    member.UserId == ownerId))
            .OrderByDescending(project => project.CreatedAtUtc)
            .Select(project => new AcademicProjectResult(
                project.Id,
                project.CourseId,
                project.Title,
                project.Description,
                project.DueDateUtc,
                project.Status.ToString(),
                project.CreatedAtUtc,
                project.Course.OwnerId == ownerId
                    ? "Owner"
                    : project.Members
                        .Where(member =>
                            member.UserId == ownerId)
                        .Select(member =>
                            member.Role ==
                                UniPilot.Domain.Projects
                                    .ProjectMemberRole.Editor
                                ? "Editor"
                                : "Viewer")
                        .First()))
            .ToListAsync(cancellationToken);
    }

    public async Task<AcademicProjectResult?> UpdateAsync(
        UpdateAcademicProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.AcademicProjects
            .Include(existingProject => existingProject.Course)
            .Include(existingProject => existingProject.Members)
            .SingleOrDefaultAsync(
                existingProject =>
                    existingProject.Id == command.AcademicProjectId &&
                    existingProject.CourseId == command.CourseId,
                cancellationToken);

        var isOwner =
            project?.Course.OwnerId == command.OwnerId;

        var isEditor =
            project?.Members.Any(member =>
                member.UserId == command.OwnerId &&
                member.Role ==
                    UniPilot.Domain.Projects.ProjectMemberRole.Editor)
            == true;

        if (project is null || (!isOwner && !isEditor))
        {
            return null;
        }

        var previousStatus = project.Status;

        project.Title = command.Title.Trim();
        project.Description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();
        project.DueDateUtc = command.DueDateUtc?.ToUniversalTime();
        project.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousStatus != project.Status)
        {
            await activityService.RecordAsync(
                command.OwnerId,
                project.Id,
                ProjectActivityType.ProjectStatusChanged,
                "Project status changed",
                $"{project.Title} moved from {previousStatus} " +
                $"to {project.Status}.",
                cancellationToken);
        }

        return Map(
            project,
            isOwner ? "Owner" : "Editor");
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.AcademicProjects
            .Include(existingProject => existingProject.Documents)
            .SingleOrDefaultAsync(
                existingProject =>
                    existingProject.Id == academicProjectId &&
                    existingProject.Course.OwnerId == ownerId,
                cancellationToken);

        if (project is null)
        {
            return false;
        }

        var storageKeys = project.Documents
            .Select(document => document.StorageKey)
            .Where(storageKey => !string.IsNullOrWhiteSpace(storageKey))
            .ToList();

        dbContext.AcademicProjects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var storageKey in storageKeys)
        {
            try
            {
                await fileStorage.DeleteAsync(
                    storageKey,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Unable to delete stored project document {StorageKey}.",
                    storageKey);
            }
        }

        return true;
    }

    private static AcademicProjectResult Map(
        AcademicProject project,
        string accessRole)
    {
        return new AcademicProjectResult(
            project.Id,
            project.CourseId,
            project.Title,
            project.Description,
            project.DueDateUtc,
            project.Status.ToString(),
            project.CreatedAtUtc,
            accessRole);
    }
}
