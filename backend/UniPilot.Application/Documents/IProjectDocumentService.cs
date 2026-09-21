namespace UniPilot.Application.Documents;

public interface IProjectDocumentService
{
    Task<UploadDocumentResult> UploadAsync(
        UploadProjectDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectDocumentResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentPageResult>?>
        GetPagesAsync(
            Guid ownerId,
            Guid documentId,
            CancellationToken cancellationToken = default);

    Task<ProjectDocumentFileResult?> GetFileAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}