namespace UniPilot.Application.Projects;

public sealed record AcademicProjectResult(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    string Status,
    DateTime CreatedAtUtc);