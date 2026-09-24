using UniPilot.Application.ProjectMembers;

namespace UniPilot.Tests.Infrastructure;

public sealed class TestProjectInvitationEmailSender
    : IProjectInvitationEmailSender
{
    public Task SendProjectInvitationAsync(
        string email,
        string inviterName,
        string projectTitle,
        string role,
        string invitationLink,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}