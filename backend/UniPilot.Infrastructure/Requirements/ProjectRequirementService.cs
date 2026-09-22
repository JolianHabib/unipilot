using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Activities;
using UniPilot.Application.Requirements;
using UniPilot.Domain.Activities;
using UniPilot.Domain.Entities;
using UniPilot.Domain.Tasks;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Requirements;

public sealed class ProjectRequirementService(
    AppDbContext dbContext,
    IRequirementExtractor requirementExtractor,
    IProjectActivityService activityService)
    : IProjectRequirementService
{
    public async Task<IReadOnlyList<ProjectRequirementResult>?>
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

        await RemoveDuplicateRequirementsAsync(
            ownerId,
            academicProjectId,
            cancellationToken);

        return await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.AcademicProjectId == academicProjectId)
            .OrderBy(requirement => requirement.SourcePageNumber)
            .ThenBy(requirement => requirement.CreatedAtUtc)
            .Select(ResultProjection)
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
                requirement.AcademicProject.Course.OwnerId == ownerId)
            .OrderByDescending(requirement => requirement.CreatedAtUtc)
            .Select(ResultProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectRequirementResult?> CreateAsync(
        Guid ownerId,
        Guid academicProjectId,
        CreateProjectRequirementCommand command,
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

        var requirement = new ProjectRequirement
        {
            AcademicProjectId = academicProjectId,
            ProjectDocumentId = null,
            SourcePageNumber = null,
            Title = Truncate(command.Title.Trim(), 250),
            Description = command.Description.Trim(),
            Type = command.Type,
            Priority = command.Priority,
            IsCompleted = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.ProjectRequirements.Add(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.RequirementCreated,
            "Requirement created",
            $"{requirement.Title} was created manually.",
            cancellationToken);

        return MapResult(requirement);
    }

    public async Task<IReadOnlyList<ProjectRequirementResult>?>
        ExtractFromDocumentAsync(
            Guid ownerId,
            Guid documentId,
            CancellationToken cancellationToken = default)
    {
        var document = await dbContext.ProjectDocuments
            .Include(projectDocument => projectDocument.Pages)
            .SingleOrDefaultAsync(
                projectDocument =>
                    projectDocument.Id == documentId &&
                    projectDocument.AcademicProject.Course.OwnerId == ownerId,
                cancellationToken);

        if (document is null)
        {
            return null;
        }

        if (document.ProcessingStatus != DocumentProcessingStatus.Ready)
        {
            throw new InvalidOperationException(
                "The document is not ready for requirement extraction.");
        }

        var existingRequirements = await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.ProjectDocumentId == documentId)
            .OrderBy(requirement => requirement.SourcePageNumber)
            .ThenBy(requirement => requirement.CreatedAtUtc)
            .Select(ResultProjection)
            .ToListAsync(cancellationToken);

        if (existingRequirements.Count > 0)
        {
            return existingRequirements;
        }

        var sourcePages = document.Pages
            .OrderBy(page => page.PageNumber)
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .Select(page => new RequirementSourcePage(
                page.PageNumber,
                page.Text))
            .ToList();

        var extractedRequirements = await requirementExtractor.ExtractAsync(
            sourcePages,
            cancellationToken);

        var projectRequirementValues = await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.AcademicProjectId == document.AcademicProjectId)
            .Select(requirement => new
            {
                requirement.Title,
                requirement.Description
            })
            .ToListAsync(cancellationToken);

        var knownRequirementKeys = projectRequirementValues
            .Select(requirement => CreateComparisonKey(
                requirement.Title,
                requirement.Description))
            .ToHashSet(StringComparer.Ordinal);

        var addedCount = 0;

        foreach (var extracted in extractedRequirements)
        {
            if (string.IsNullOrWhiteSpace(extracted.Title) ||
                string.IsNullOrWhiteSpace(extracted.Description))
            {
                continue;
            }

            var title = Truncate(extracted.Title.Trim(), 250);
            var description = extracted.Description.Trim();

            if (!knownRequirementKeys.Add(
                    CreateComparisonKey(title, description)))
            {
                continue;
            }

            dbContext.ProjectRequirements.Add(new ProjectRequirement
            {
                AcademicProjectId = document.AcademicProjectId,
                ProjectDocumentId = document.Id,
                SourcePageNumber = extracted.SourcePageNumber,
                Title = title,
                Description = description,
                Type = extracted.Type,
                Priority = extracted.Priority
            });

            addedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (addedCount > 0)
        {
            await activityService.RecordAsync(
                ownerId,
                document.AcademicProjectId,
                ProjectActivityType.RequirementsExtracted,
                "Requirements extracted",
                $"{addedCount} requirement(s) were extracted from " +
                $"{document.OriginalFileName}.",
                cancellationToken);
        }

        return await dbContext.ProjectRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.ProjectDocumentId == documentId)
            .OrderBy(requirement => requirement.SourcePageNumber)
            .ThenBy(requirement => requirement.CreatedAtUtc)
            .Select(ResultProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectRequirementResult?> SetCompletionAsync(
        Guid ownerId,
        Guid requirementId,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var requirement = await dbContext.ProjectRequirements
            .SingleOrDefaultAsync(
                item =>
                    item.Id == requirementId &&
                    item.AcademicProject.Course.OwnerId == ownerId,
                cancellationToken);

        if (requirement is null)
        {
            return null;
        }

        var completionChanged =
            requirement.IsCompleted != isCompleted;

        requirement.IsCompleted = isCompleted;

        var projectTasks = await dbContext.ProjectTasks
            .Where(task =>
                task.AcademicProjectId == requirement.AcademicProjectId)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var targetStatus = isCompleted
            ? ProjectTaskStatus.Done
            : ProjectTaskStatus.ToDo;

        var now = DateTime.UtcNow;

        foreach (var task in projectTasks.Where(task =>
                     task.ProjectRequirementId == requirement.Id &&
                     task.Status != targetStatus))
        {
            task.Status = targetStatus;
            task.Position = int.MaxValue;
            task.UpdatedAtUtc = now;
        }

        NormalizeTaskPositions(projectTasks);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (completionChanged)
        {
            await activityService.RecordAsync(
                ownerId,
                requirement.AcademicProjectId,
                isCompleted
                    ? ProjectActivityType.RequirementCompleted
                    : ProjectActivityType.RequirementReopened,
                isCompleted
                    ? "Requirement completed"
                    : "Requirement reopened",
                isCompleted
                    ? $"{requirement.Title} was marked as completed."
                    : $"{requirement.Title} was marked as incomplete.",
                cancellationToken);
        }

        return MapResult(requirement);
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid requirementId,
        CancellationToken cancellationToken = default)
    {
        var requirement = await dbContext.ProjectRequirements
            .SingleOrDefaultAsync(
                item =>
                    item.Id == requirementId &&
                    item.AcademicProject.Course.OwnerId == ownerId,
                cancellationToken);

        if (requirement is null)
        {
            return false;
        }

        var academicProjectId =
            requirement.AcademicProjectId;

        var deletedTitle = requirement.Title;

        dbContext.ProjectRequirements.Remove(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.RequirementDeleted,
            "Requirement deleted",
            $"{deletedTitle} was deleted.",
            cancellationToken);

        return true;
    }

    public async Task<ProjectRequirementResult?> UpdateAsync(
        Guid ownerId,
        Guid requirementId,
        UpdateProjectRequirementCommand command,
        CancellationToken cancellationToken = default)
    {
        var requirement = await dbContext.ProjectRequirements
            .SingleOrDefaultAsync(
                item =>
                    item.Id == requirementId &&
                    item.AcademicProject.Course.OwnerId == ownerId,
                cancellationToken);

        if (requirement is null)
        {
            return null;
        }

        requirement.Title = Truncate(command.Title.Trim(), 250);
        requirement.Description = command.Description.Trim();
        requirement.Type = command.Type;
        requirement.Priority = command.Priority;

        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            requirement.AcademicProjectId,
            ProjectActivityType.RequirementUpdated,
            "Requirement updated",
            $"{requirement.Title} was updated.",
            cancellationToken);

        return MapResult(requirement);
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

            for (var index = 0; index < tasksInStatus.Count; index++)
            {
                tasksInStatus[index].Position = index;
            }
        }
    }

    private static ProjectRequirementResult MapResult(
        ProjectRequirement requirement)
    {
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

    private static readonly Expression<
        Func<ProjectRequirement, ProjectRequirementResult>>
        ResultProjection = requirement =>
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
                requirement.CreatedAtUtc);

    private static string Truncate(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private static string CreateComparisonKey(
        string title,
        string description)
    {
        return $"{NormalizeForComparison(title)}\u001f" +
            NormalizeForComparison(description);
    }

    private static string NormalizeForComparison(string value)
    {
        return string.Join(
                ' ',
                value.Trim().Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private async Task RemoveDuplicateRequirementsAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken)
    {
        var requirements = await dbContext.ProjectRequirements
            .Include(requirement => requirement.Tasks)
            .Where(requirement =>
                requirement.AcademicProjectId == academicProjectId)
            .OrderBy(requirement => requirement.CreatedAtUtc)
            .ThenBy(requirement => requirement.Id)
            .ToListAsync(cancellationToken);

        var duplicateGroups = requirements
            .GroupBy(requirement => CreateComparisonKey(
                requirement.Title,
                requirement.Description))
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicateGroups.Count == 0)
        {
            return;
        }

        var projectTasks = await dbContext.ProjectTasks
            .Where(task =>
                task.AcademicProjectId == academicProjectId)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var removedCount = 0;
        var now = DateTime.UtcNow;

        foreach (var group in duplicateGroups)
        {
            var canonical = group.First();
            var duplicates = group.Skip(1).ToList();
            var isCompleted = group.Any(requirement =>
                requirement.IsCompleted);

            canonical.IsCompleted = isCompleted;

            foreach (var duplicate in duplicates)
            {
                foreach (var task in duplicate.Tasks.ToList())
                {
                    task.ProjectRequirementId = canonical.Id;
                    task.ProjectRequirement = canonical;
                }

                dbContext.ProjectRequirements.Remove(duplicate);
                removedCount++;
            }

            if (isCompleted)
            {
                foreach (var task in projectTasks.Where(task =>
                             task.ProjectRequirementId == canonical.Id))
                {
                    task.Status = ProjectTaskStatus.Done;
                    task.Position = int.MaxValue;
                    task.UpdatedAtUtc = now;
                }
            }
        }

        NormalizeTaskPositions(projectTasks);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.RequirementDuplicatesRemoved,
            "Duplicate requirements removed",
            $"{removedCount} duplicate requirement(s) were merged safely.",
            cancellationToken);
    }
}
