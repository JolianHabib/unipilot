namespace UniPilot.Application.Projects;

public sealed record CreateAcademicProjectCommand(
    Guid OwnerId,
    Guid CourseId,
    string Title,
    string? Description,
    DateTime? DueDateUtc);