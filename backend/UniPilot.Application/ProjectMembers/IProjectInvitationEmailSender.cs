namespace UniPilot.Application.ProjectMembers;

public interface IProjectInvitationEmailSender
{
    Task SendProjectInvitationAsync(
        string email,
        string inviterName,
        string projectTitle,
        string role,
        string invitationLink,
        CancellationToken cancellationToken = default);
}
