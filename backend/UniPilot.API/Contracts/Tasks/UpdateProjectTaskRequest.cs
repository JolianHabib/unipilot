namespace UniPilot.API.Contracts.Tasks;

public sealed record UpdateProjectTaskRequest(
    Guid? ProjectRequirementId,
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDateUtc);