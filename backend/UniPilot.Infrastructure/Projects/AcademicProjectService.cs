using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniPilot.Application.Documents;
using UniPilot.Application.Projects;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Projects;

public sealed class AcademicProjectService(
    AppDbContext dbContext,
    IFileStorage fileStorage,
    ILogger<AcademicProjectService> logger)
    : IAcademicProjectService
{
    public async Task<AcademicProjectResult?> CreateAsync(
        CreateAcademicProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownsCourse =
            await dbContext.Courses.AnyAsync(
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
            Description =
                string.IsNullOrWhiteSpace(
                    command.Description)
                    ? null
                    : command.Description.Trim(),
            DueDateUtc =
                command.DueDateUtc
                    ?.ToUniversalTime(),
            Status =
                AcademicProjectStatus.Draft
        };

        dbContext.AcademicProjects.Add(
            project);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return Map(project);
    }

    public async Task<
        IReadOnlyList<AcademicProjectResult>?>
        GetByCourseAsync(
            Guid ownerId,
            Guid courseId,
            CancellationToken cancellationToken = default)
    {
        var ownsCourse =
            await dbContext.Courses.AnyAsync(
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
            .Where(project =>
                project.CourseId == courseId)
            .OrderByDescending(project =>
                project.CreatedAtUtc)
            .Select(project =>
                new AcademicProjectResult(
                    project.Id,
                    project.CourseId,
                    project.Title,
                    project.Description,
                    project.DueDateUtc,
                    project.Status.ToString(),
                    project.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
public async Task<
    IReadOnlyList<AcademicProjectResult>>
    GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
{
    return await dbContext.AcademicProjects
        .AsNoTracking()
        .Where(project =>
            project.Course.OwnerId == ownerId)
        .OrderByDescending(project =>
            project.CreatedAtUtc)
        .Select(project =>
            new AcademicProjectResult(
                project.Id,
                project.CourseId,
                project.Title,
                project.Description,
                project.DueDateUtc,
                project.Status.ToString(),
                project.CreatedAtUtc))
        .ToListAsync(cancellationToken);
}
    public async Task<AcademicProjectResult?>
        UpdateAsync(
            UpdateAcademicProjectCommand command,
            CancellationToken cancellationToken = default)
    {
        var project =
    await dbContext.AcademicProjects
        .SingleOrDefaultAsync(
            existingProject =>
                existingProject.Id ==
                    command.AcademicProjectId &&
                existingProject.CourseId ==
                    command.CourseId &&
                existingProject.Course.OwnerId ==
                    command.OwnerId,
            cancellationToken);

        if (project is null)
        {
            return null;
        }

        project.Title =
            command.Title.Trim();

        project.Description =
            string.IsNullOrWhiteSpace(
                command.Description)
                ? null
                : command.Description.Trim();

        project.DueDateUtc =
            command.DueDateUtc
                ?.ToUniversalTime();

        project.Status = command.Status;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return Map(project);
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default)
    {
        var project =
            await dbContext.AcademicProjects
                .Include(existingProject =>
                    existingProject.Documents)
                .SingleOrDefaultAsync(
                    existingProject =>
                        existingProject.Id ==
                            academicProjectId &&
                        existingProject.Course.OwnerId ==
                            ownerId,
                    cancellationToken);

        if (project is null)
        {
            return false;
        }

        var storageKeys =
            project.Documents
                .Select(document =>
                    document.StorageKey)
                .Where(storageKey =>
                    !string.IsNullOrWhiteSpace(
                        storageKey))
                .ToList();

        dbContext.AcademicProjects.Remove(
            project);

        await dbContext.SaveChangesAsync(
            cancellationToken);

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
        AcademicProject project)
    {
        return new AcademicProjectResult(
            project.Id,
            project.CourseId,
            project.Title,
            project.Description,
            project.DueDateUtc,
            project.Status.ToString(),
            project.CreatedAtUtc);
    }
}