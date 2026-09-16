using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Courses;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Courses;

public sealed class CourseService(AppDbContext dbContext)
    : ICourseService
{
    public async Task<CourseResult> CreateAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var course = new Course
        {
            Name = command.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(command.Code)
                ? null
                : command.Code.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
            OwnerId = command.OwnerId
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(course);
    }

    public async Task<IReadOnlyList<CourseResult>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.OwnerId == ownerId)
            .OrderByDescending(course => course.CreatedAtUtc)
            .Select(course => new CourseResult(
                course.Id,
                course.Name,
                course.Code,
                course.Description,
                course.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static CourseResult Map(Course course)
    {
        return new CourseResult(
            course.Id,
            course.Name,
            course.Code,
            course.Description,
            course.CreatedAtUtc);
    }
}