namespace UniPilot.Application.Activities;

public sealed record ProjectActivityResult(
    Guid Id,
    Guid AcademicProjectId,
    string Type,
    string Title,
    string? Description,
    DateTime CreatedAtUtc);
