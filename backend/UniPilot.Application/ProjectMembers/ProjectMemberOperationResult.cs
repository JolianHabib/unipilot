namespace UniPilot.Application.ProjectMembers;

public sealed record ProjectMemberOperationResult(
    ProjectMemberOperationStatus Status,
    ProjectMemberResult? Member = null);