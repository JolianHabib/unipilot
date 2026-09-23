namespace UniPilot.Application.ProjectMembers;

public sealed record ProjectMemberResult(
    Guid Id,
    Guid AcademicProjectId,
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    DateTime JoinedAtUtc);
