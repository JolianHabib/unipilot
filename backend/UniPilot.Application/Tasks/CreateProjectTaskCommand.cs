using UniPilot.Domain.Tasks;

namespace UniPilot.Application.Tasks;

public sealed record CreateProjectTaskCommand(
    Guid OwnerId,
    Guid AcademicProjectId,
    Guid? ProjectRequirementId,
    string Title,
    string? Description,
    ProjectTaskPriority Priority,
    DateTime? DueDateUtc);