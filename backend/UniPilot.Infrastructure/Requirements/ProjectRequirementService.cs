using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Requirements;
using UniPilot.Domain.Entities;
using UniPilot.Domain.Tasks;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Requirements;

public sealed class ProjectRequirementService(
    AppDbContext dbContext,
    IRequirementExtractor requirementExtractor)
    : IProjectRequirementService
{
    public async Task<IReadOnlyList<ProjectRequirementResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default)
    {
        var ownsProject =
            await dbContext.AcademicProjects.AnyAsync(
                project =>
                    project.Id == academicProjectId &&
                    project.Course.OwnerId == ownerId,
                cancellationToken);

        if (!ownsProject)
        {
            return null;
        }

        return await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.AcademicProjectId ==
                    academicProjectId)
            .OrderBy(requirement =>
                requirement.SourcePageNumber)
            .ThenBy(requirement =>
                requirement.CreatedAtUtc)
            .Select(requirement =>
                new ProjectRequirementResult(
                    requirement.Id,
                    requirement.AcademicProjectId,
                    requirement.ProjectDocumentId,
                    requirement.SourcePageNumber,
                    requirement.Title,
                    requirement.Description,
                    requirement.Type.ToString(),
                    requirement.Priority.ToString(),
                    requirement.IsCompleted,
                    requirement.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectRequirementResult>>
        GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement
                    .AcademicProject
                    .Course
                    .OwnerId == ownerId)
            .OrderByDescending(requirement =>
                requirement.CreatedAtUtc)
            .Select(requirement =>
                new ProjectRequirementResult(
                    requirement.Id,
                    requirement.AcademicProjectId,
                    requirement.ProjectDocumentId,
                    requirement.SourcePageNumber,
                    requirement.Title,
                    requirement.Description,
                    requirement.Type.ToString(),
                    requirement.Priority.ToString(),
                    requirement.IsCompleted,
                    requirement.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectRequirementResult>?>
        ExtractFromDocumentAsync(
            Guid ownerId,
            Guid documentId,
            CancellationToken cancellationToken = default)
    {
        var document =
            await dbContext.ProjectDocuments
                .Include(projectDocument =>
                    projectDocument.Pages)
                .SingleOrDefaultAsync(
                    projectDocument =>
                        projectDocument.Id == documentId &&
                        projectDocument.AcademicProject
                            .Course.OwnerId == ownerId,
                    cancellationToken);

        if (document is null)
        {
            return null;
        }

        if (document.ProcessingStatus !=
            DocumentProcessingStatus.Ready)
        {
            throw new InvalidOperationException(
                "The document is not ready for requirement extraction.");
        }

        var existingRequirements =
            await dbContext.ProjectRequirements
                .AsNoTracking()
                .Where(requirement =>
                    requirement.ProjectDocumentId ==
                        documentId)
                .OrderBy(requirement =>
                    requirement.SourcePageNumber)
                .ThenBy(requirement =>
                    requirement.CreatedAtUtc)
                .Select(requirement =>
                    new ProjectRequirementResult(
                        requirement.Id,
                        requirement.AcademicProjectId,
                        requirement.ProjectDocumentId,
                        requirement.SourcePageNumber,
                        requirement.Title,
                        requirement.Description,
                        requirement.Type.ToString(),
                        requirement.Priority.ToString(),
                        requirement.IsCompleted,
                        requirement.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        if (existingRequirements.Count > 0)
        {
            return existingRequirements;
        }

        var sourcePages =
            document.Pages
                .OrderBy(page => page.PageNumber)
                .Where(page =>
                    !string.IsNullOrWhiteSpace(page.Text))
                .Select(page =>
                    new RequirementSourcePage(
                        page.PageNumber,
                        page.Text))
                .ToList();

        var extractedRequirements =
            await requirementExtractor.ExtractAsync(
                sourcePages,
                cancellationToken);

        foreach (var extracted in extractedRequirements)
        {
            if (string.IsNullOrWhiteSpace(extracted.Title) ||
                string.IsNullOrWhiteSpace(
                    extracted.Description))
            {
                continue;
            }

            dbContext.ProjectRequirements.Add(
                new ProjectRequirement
                {
                    AcademicProjectId =
                        document.AcademicProjectId,

                    ProjectDocumentId =
                        document.Id,

                    SourcePageNumber =
                        extracted.SourcePageNumber,

                    Title =
                        Truncate(extracted.Title.Trim(), 250),

                    Description =
                        extracted.Description.Trim(),

                    Type =
                        extracted.Type,

                    Priority =
                        extracted.Priority
                });
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.ProjectDocumentId ==
                    documentId)
            .OrderBy(requirement =>
                requirement.SourcePageNumber)
            .ThenBy(requirement =>
                requirement.CreatedAtUtc)
            .Select(requirement =>
                new ProjectRequirementResult(
                    requirement.Id,
                    requirement.AcademicProjectId,
                    requirement.ProjectDocumentId,
                    requirement.SourcePageNumber,
                    requirement.Title,
                    requirement.Description,
                    requirement.Type.ToString(),
                    requirement.Priority.ToString(),
                    requirement.IsCompleted,
                    requirement.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectRequirementResult?>
        SetCompletionAsync(
            Guid ownerId,
            Guid requirementId,
            bool isCompleted,
            CancellationToken cancellationToken = default)
    {
        var requirement =
            await dbContext.ProjectRequirements
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == requirementId &&
                        item.AcademicProject.Course.OwnerId ==
                            ownerId,
                    cancellationToken);

        if (requirement is null)
        {
            return null;
        }

        requirement.IsCompleted = isCompleted;

        var projectTasks =
            await dbContext.ProjectTasks
                .Where(task =>
                    task.AcademicProjectId ==
                        requirement.AcademicProjectId)
                .OrderBy(task => task.Position)
                .ThenBy(task => task.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        var targetStatus = isCompleted
            ? ProjectTaskStatus.Done
            : ProjectTaskStatus.ToDo;

        var now = DateTime.UtcNow;

        foreach (var task in projectTasks.Where(task =>
                     task.ProjectRequirementId ==
                         requirement.Id &&
                     task.Status != targetStatus))
        {
            task.Status = targetStatus;
            task.Position = int.MaxValue;
            task.UpdatedAtUtc = now;
        }

        NormalizeTaskPositions(projectTasks);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new ProjectRequirementResult(
            requirement.Id,
            requirement.AcademicProjectId,
            requirement.ProjectDocumentId,
            requirement.SourcePageNumber,
            requirement.Title,
            requirement.Description,
            requirement.Type.ToString(),
            requirement.Priority.ToString(),
            requirement.IsCompleted,
            requirement.CreatedAtUtc);
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid requirementId,
        CancellationToken cancellationToken = default)
    {
        var requirement =
            await dbContext.ProjectRequirements
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == requirementId &&
                        item.AcademicProject.Course.OwnerId ==
                            ownerId,
                    cancellationToken);

        if (requirement is null)
        {
            return false;
        }

        dbContext.ProjectRequirements.Remove(requirement);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<ProjectRequirementResult?>
        UpdateAsync(
            Guid ownerId,
            Guid requirementId,
            UpdateProjectRequirementCommand command,
            CancellationToken cancellationToken = default)
    {
        var requirement =
            await dbContext.ProjectRequirements
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == requirementId &&
                        item.AcademicProject.Course.OwnerId ==
                            ownerId,
                    cancellationToken);

        if (requirement is null)
        {
            return null;
        }

        requirement.Title =
            Truncate(command.Title.Trim(), 250);

        requirement.Description =
            command.Description.Trim();

        requirement.Type = command.Type;
        requirement.Priority = command.Priority;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new ProjectRequirementResult(
            requirement.Id,
            requirement.AcademicProjectId,
            requirement.ProjectDocumentId,
            requirement.SourcePageNumber,
            requirement.Title,
            requirement.Description,
            requirement.Type.ToString(),
            requirement.Priority.ToString(),
            requirement.IsCompleted,
            requirement.CreatedAtUtc);
    }

    private static void NormalizeTaskPositions(
        IReadOnlyList<ProjectTask> projectTasks)
    {
        foreach (var status in Enum.GetValues<ProjectTaskStatus>())
        {
            var tasksInStatus = projectTasks
                .Where(task => task.Status == status)
                .OrderBy(task => task.Position)
                .ThenBy(task => task.CreatedAtUtc)
                .ToList();

            for (var index = 0;
                 index < tasksInStatus.Count;
                 index++)
            {
                tasksInStatus[index].Position = index;
            }
        }
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}
