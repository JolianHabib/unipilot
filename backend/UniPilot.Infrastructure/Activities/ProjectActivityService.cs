using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Activities;
using UniPilot.Domain.Activities;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Activities;

public sealed class ProjectActivityService(
    AppDbContext dbContext) : IProjectActivityService
{
    public async Task<IReadOnlyList<ProjectActivityResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default)
    {
        var ownsProject = await dbContext.AcademicProjects.AnyAsync(
            project =>
                project.Id == academicProjectId &&
                project.Course.OwnerId == ownerId,
            cancellationToken);

        if (!ownsProject)
        {
            return null;
        }

        return await dbContext.ProjectActivities
            .AsNoTracking()
            .Where(activity =>
                activity.AcademicProjectId == academicProjectId)
            .OrderByDescending(activity => activity.CreatedAtUtc)
            .Select(activity => new ProjectActivityResult(
                activity.Id,
                activity.AcademicProjectId,
                activity.Type.ToString(),
                activity.Title,
                activity.Description,
                activity.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task RecordAsync(
        Guid userId,
        Guid academicProjectId,
        ProjectActivityType type,
        string title,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.ProjectActivities.Add(new ProjectActivity
        {
            UserId = userId,
            AcademicProjectId = academicProjectId,
            Type = type,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
