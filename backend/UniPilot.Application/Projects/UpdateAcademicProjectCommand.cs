namespace UniPilot.Application.Projects;

public sealed record UpdateAcademicProjectCommand(
    Guid OwnerId,
    Guid AcademicProjectId,
    string Title,
    string? Description,
    DateTime? DueDateUtc);