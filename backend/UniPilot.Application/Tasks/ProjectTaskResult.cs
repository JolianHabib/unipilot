namespace UniPilot.Application.Tasks;

public sealed record ProjectTaskResult(
    Guid Id,
    Guid AcademicProjectId,
    Guid? ProjectRequirementId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime? DueDateUtc,
    int Position,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);