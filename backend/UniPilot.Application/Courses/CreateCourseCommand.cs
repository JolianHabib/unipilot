namespace UniPilot.Application.Courses;

public sealed record CreateCourseCommand(
    Guid OwnerId,
    string Name,
    string? Code,
    string? Description);