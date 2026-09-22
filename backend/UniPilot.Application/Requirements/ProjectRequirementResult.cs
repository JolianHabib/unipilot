namespace UniPilot.Application.Requirements;

public sealed record ProjectRequirementResult(
    Guid Id,
    Guid AcademicProjectId,
    Guid? ProjectDocumentId,
    int? SourcePageNumber,
    string Title,
    string Description,
    string Type,
    string Priority,
    bool IsCompleted,
    DateTime CreatedAtUtc);