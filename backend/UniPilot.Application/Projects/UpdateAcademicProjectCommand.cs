using UniPilot.Domain.Entities;

namespace UniPilot.Application.Projects;

public sealed record UpdateAcademicProjectCommand(
    Guid OwnerId,
    Guid CourseId,
    Guid AcademicProjectId,
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    AcademicProjectStatus Status);