namespace UniPilot.Application.Documents;

public sealed record ProjectDocumentResult(
    Guid Id,
    Guid AcademicProjectId,
    string OriginalFileName,
    string DocumentType,
    string ProcessingStatus,
    long FileSizeBytes,
    int PageCount,
    string? FailureReason,
    DateTime UploadedAtUtc);