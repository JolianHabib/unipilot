using UniPilot.Domain.Entities;

namespace UniPilot.Application.Documents;

public sealed record UploadProjectDocumentCommand(
    Guid OwnerId,
    Guid AcademicProjectId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    ProjectDocumentType DocumentType,
    Stream Content);