namespace UniPilot.Application.ProjectMembers;

public sealed record ProjectInvitationAcceptanceResult(
    ProjectInvitationAcceptanceStatus Status,
    Guid? AcademicProjectId = null);
