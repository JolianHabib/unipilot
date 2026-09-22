using UniPilot.Domain.Tasks;

namespace UniPilot.Application.Tasks;

public sealed record MoveProjectTaskCommand(
    Guid OwnerId,
    Guid AcademicProjectId,
    Guid ProjectTaskId,
    ProjectTaskStatus Status,
    int Position);