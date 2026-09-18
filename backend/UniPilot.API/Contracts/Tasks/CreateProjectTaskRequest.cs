namespace UniPilot.API.Contracts.Tasks;

public sealed record CreateProjectTaskRequest(
    Guid? ProjectRequirementId,
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDateUtc);