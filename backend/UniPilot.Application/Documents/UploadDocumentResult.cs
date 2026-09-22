namespace UniPilot.Application.Documents;

public sealed record UploadDocumentResult(
    UploadDocumentStatus Status,
    ProjectDocumentResult? Document);