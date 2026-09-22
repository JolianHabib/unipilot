using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Activities;
using UniPilot.Application.Tasks;
using UniPilot.Domain.Activities;
using UniPilot.Domain.Entities;
using UniPilot.Domain.Tasks;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Tasks;

public sealed class ProjectTaskService(
    AppDbContext dbContext,
    IProjectActivityService activityService)
    : IProjectTaskService
{
    public async Task<IReadOnlyList<ProjectTaskResult>?>
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

        return await dbContext.ProjectTasks
            .AsNoTracking()
            .Where(task =>
                task.AcademicProjectId == academicProjectId)
            .OrderBy(task => task.Status)
            .ThenBy(task => task.Position)
            .ThenBy(task => task.CreatedAtUtc)
            .Select(task => new ProjectTaskResult(
                task.Id,
                task.AcademicProjectId,
                task.ProjectRequirementId,
                task.Title,
                task.Description,
                task.Status.ToString(),
                task.Priority.ToString(),
                task.DueDateUtc,
                task.Position,
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectTaskResult?> CreateAsync(
        CreateProjectTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(
                command.OwnerId,
                command.AcademicProjectId,
                cancellationToken))
        {
            return null;
        }

        if (!await IsValidRequirementAsync(
                command.AcademicProjectId,
                command.ProjectRequirementId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The selected requirement does not belong to this project.");
        }

        var lastPosition = await dbContext.ProjectTasks
            .Where(task =>
                task.AcademicProjectId == command.AcademicProjectId &&
                task.Status == ProjectTaskStatus.ToDo)
            .Select(task => (int?)task.Position)
            .MaxAsync(cancellationToken) ?? -1;

        var now = DateTime.UtcNow;

        var projectTask = new ProjectTask
        {
            AcademicProjectId = command.AcademicProjectId,
            ProjectRequirementId = command.ProjectRequirementId,
            Title = command.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
            Status = ProjectTaskStatus.ToDo,
            Priority = command.Priority,
            DueDateUtc = command.DueDateUtc?.ToUniversalTime(),
            Position = lastPosition + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.ProjectTasks.Add(projectTask);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            command.OwnerId,
            command.AcademicProjectId,
            ProjectActivityType.TaskCreated,
            "Task created",
            $"{projectTask.Title} was added to To Do.",
            cancellationToken);

        return Map(projectTask);
    }

    public async Task<ProjectTaskResult?> UpdateAsync(
        UpdateProjectTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var projectTask = await dbContext.ProjectTasks
            .Include(task => task.AcademicProject)
            .ThenInclude(project => project.Course)
            .SingleOrDefaultAsync(
                task =>
                    task.Id == command.ProjectTaskId &&
                    task.AcademicProjectId == command.AcademicProjectId &&
                    task.AcademicProject.Course.OwnerId == command.OwnerId,
                cancellationToken);

        if (projectTask is null)
        {
            return null;
        }

        if (!await IsValidRequirementAsync(
                command.AcademicProjectId,
                command.ProjectRequirementId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The selected requirement does not belong to this project.");
        }

        projectTask.ProjectRequirementId = command.ProjectRequirementId;
        projectTask.Title = command.Title.Trim();
        projectTask.Description =
            string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();
        projectTask.Priority = command.Priority;
        projectTask.DueDateUtc = command.DueDateUtc?.ToUniversalTime();
        projectTask.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            command.OwnerId,
            command.AcademicProjectId,
            ProjectActivityType.TaskUpdated,
            "Task updated",
            $"{projectTask.Title} was updated.",
            cancellationToken);

        return Map(projectTask);
    }

    public async Task<ProjectTaskResult?> MoveAsync(
        MoveProjectTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsProjectAsync(
                command.OwnerId,
                command.AcademicProjectId,
                cancellationToken))
        {
            return null;
        }

        var projectTasks = await dbContext.ProjectTasks
            .Where(task =>
                task.AcademicProjectId == command.AcademicProjectId)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var projectTask = projectTasks.SingleOrDefault(
            task => task.Id == command.ProjectTaskId);

        if (projectTask is null)
        {
            return null;
        }

        var previousStatus = projectTask.Status;

        projectTask.Status = command.Status;
        projectTask.UpdatedAtUtc = DateTime.UtcNow;

        if (projectTask.ProjectRequirementId is Guid requirementId)
        {
            var linkedRequirement =
                await dbContext.ProjectRequirements.SingleOrDefaultAsync(
                    requirement =>
                        requirement.Id == requirementId &&
                        requirement.AcademicProjectId ==
                            command.AcademicProjectId,
                    cancellationToken);

            if (linkedRequirement is not null)
            {
                linkedRequirement.IsCompleted =
                    command.Status == ProjectTaskStatus.Done;
            }
        }

        var targetTasks = projectTasks
            .Where(task =>
                task.Id != projectTask.Id &&
                task.Status == command.Status)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.CreatedAtUtc)
            .ToList();

        var targetPosition = Math.Clamp(
            command.Position,
            0,
            targetTasks.Count);

        targetTasks.Insert(targetPosition, projectTask);

        for (var index = 0; index < targetTasks.Count; index++)
        {
            targetTasks[index].Position = index;
        }

        if (previousStatus != command.Status)
        {
            var previousTasks = projectTasks
                .Where(task =>
                    task.Id != projectTask.Id &&
                    task.Status == previousStatus)
                .OrderBy(task => task.Position)
                .ThenBy(task => task.CreatedAtUtc)
                .ToList();

            for (var index = 0; index < previousTasks.Count; index++)
            {
                previousTasks[index].Position = index;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            command.OwnerId,
            command.AcademicProjectId,
            ProjectActivityType.TaskMoved,
            "Task moved",
            previousStatus == command.Status
                ? $"{projectTask.Title} was reordered in " +
                  $"{FormatStatus(command.Status)}."
                : $"{projectTask.Title} moved from " +
                  $"{FormatStatus(previousStatus)} to " +
                  $"{FormatStatus(command.Status)}.",
            cancellationToken);

        return Map(projectTask);
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid projectTaskId,
        CancellationToken cancellationToken = default)
    {
        var projectTask = await dbContext.ProjectTasks
            .Include(task => task.AcademicProject)
            .ThenInclude(project => project.Course)
            .SingleOrDefaultAsync(
                task =>
                    task.Id == projectTaskId &&
                    task.AcademicProjectId == academicProjectId &&
                    task.AcademicProject.Course.OwnerId == ownerId,
                cancellationToken);

        if (projectTask is null)
        {
            return false;
        }

        var deletedStatus = projectTask.Status;
        var deletedTitle = projectTask.Title;

        dbContext.ProjectTasks.Remove(projectTask);

        var remainingTasks = await dbContext.ProjectTasks
            .Where(task =>
                task.AcademicProjectId == academicProjectId &&
                task.Status == deletedStatus &&
                task.Id != projectTaskId)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < remainingTasks.Count; index++)
        {
            remainingTasks[index].Position = index;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await activityService.RecordAsync(
            ownerId,
            academicProjectId,
            ProjectActivityType.TaskDeleted,
            "Task deleted",
            $"{deletedTitle} was deleted.",
            cancellationToken);

        return true;
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

    private async Task<bool> IsValidRequirementAsync(
        Guid academicProjectId,
        Guid? projectRequirementId,
        CancellationToken cancellationToken)
    {
        if (projectRequirementId is null)
        {
            return true;
        }

        return await dbContext.ProjectRequirements.AnyAsync(
            requirement =>
                requirement.Id == projectRequirementId.Value &&
                requirement.AcademicProjectId == academicProjectId,
            cancellationToken);
    }

    private static string FormatStatus(ProjectTaskStatus status)
    {
        return status switch
        {
            ProjectTaskStatus.ToDo => "To Do",
            ProjectTaskStatus.InProgress => "In Progress",
            ProjectTaskStatus.Done => "Done",
            _ => status.ToString()
        };
    }

    private static ProjectTaskResult Map(ProjectTask projectTask)
    {
        return new ProjectTaskResult(
            projectTask.Id,
            projectTask.AcademicProjectId,
            projectTask.ProjectRequirementId,
            projectTask.Title,
            projectTask.Description,
            projectTask.Status.ToString(),
            projectTask.Priority.ToString(),
            projectTask.DueDateUtc,
            projectTask.Position,
            projectTask.CreatedAtUtc,
            projectTask.UpdatedAtUtc);
    }
}
