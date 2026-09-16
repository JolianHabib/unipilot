using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Projects;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Projects;

public sealed class AcademicProjectService(
    AppDbContext dbContext) : IAcademicProjectService
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
            Description =
                string.IsNullOrWhiteSpace(command.Description)
                    ? null
                    : command.Description.Trim(),
            DueDateUtc = command.DueDateUtc?.ToUniversalTime(),
            Status = AcademicProjectStatus.Draft
        };

        dbContext.AcademicProjects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(project);
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
                project.CreatedAtUtc))
            .ToListAsync(cancellationToken);
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