namespace UniPilot.Application.Courses;

public sealed record UpdateCourseCommand(
    Guid OwnerId,
    Guid CourseId,
    string Name,
    string? Code,
    string? Description);