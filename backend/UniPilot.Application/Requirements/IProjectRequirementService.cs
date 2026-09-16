namespace UniPilot.Application.Requirements;

public interface IProjectRequirementService
{
    Task<IReadOnlyList<ProjectRequirementResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectRequirementResult>?>
        ExtractFromDocumentAsync(
            Guid ownerId,
            Guid documentId,
            CancellationToken cancellationToken = default);
}