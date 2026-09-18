using UniPilot.Domain.Tasks;

namespace UniPilot.Application.Tasks;

public sealed record UpdateProjectTaskCommand(
    Guid OwnerId,
    Guid AcademicProjectId,
    Guid ProjectTaskId,
    Guid? ProjectRequirementId,
    string Title,
    string? Description,
    ProjectTaskPriority Priority,
    DateTime? DueDateUtc);