namespace UniPilot.Application.Courses;

public sealed record CourseResult(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    DateTime CreatedAtUtc);