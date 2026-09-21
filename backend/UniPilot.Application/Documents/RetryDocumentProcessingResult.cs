namespace UniPilot.Application.Documents;

public enum RetryDocumentProcessingStatus
{
    Success,
    DocumentNotFound,
    DocumentNotFailed,
    ProcessingFailed
}

public sealed record RetryDocumentProcessingResult(
    RetryDocumentProcessingStatus Status,
    ProjectDocumentResult? Document);