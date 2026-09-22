namespace UniPilot.Application.Requirements;

public interface IProjectRequirementService
{
    Task<IReadOnlyList<ProjectRequirementResult>>
        GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectRequirementResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default);

    Task<ProjectRequirementResult?> CreateAsync(
        Guid ownerId,
        Guid academicProjectId,
        CreateProjectRequirementCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectRequirementResult>?>
        ExtractFromDocumentAsync(
            Guid ownerId,
            Guid documentId,
            CancellationToken cancellationToken = default);

    Task<ProjectRequirementResult?> SetCompletionAsync(
        Guid ownerId,
        Guid requirementId,
        bool isCompleted,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ownerId,
        Guid requirementId,
        CancellationToken cancellationToken = default);

    Task<ProjectRequirementResult?> UpdateAsync(
        Guid ownerId,
        Guid requirementId,
        UpdateProjectRequirementCommand command,
        CancellationToken cancellationToken = default);
}
