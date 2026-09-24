namespace UniPilot.Application.ProjectMembers;

public enum ProjectMemberOperationStatus
{
    Success,
    ProjectNotFound,
    UserNotFound,
    OwnerCannotBeMember,
    AlreadyMember,
    MemberNotFound,
    InvitationNotPending
}
