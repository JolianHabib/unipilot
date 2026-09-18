namespace UniPilot.Application.Tasks;

public interface IProjectTaskService
{
    Task<IReadOnlyList<ProjectTaskResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default);

    Task<ProjectTaskResult?> CreateAsync(
        CreateProjectTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<ProjectTaskResult?> UpdateAsync(
        UpdateProjectTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<ProjectTaskResult?> MoveAsync(
        MoveProjectTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid projectTaskId,
        CancellationToken cancellationToken = default);
}