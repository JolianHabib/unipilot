using System.Collections.Concurrent;
using UniPilot.Application.ProjectMembers;

namespace UniPilot.Tests.Infrastructure;

public sealed class TestProjectInvitationEmailSender
    : IProjectInvitationEmailSender
{
    private readonly ConcurrentDictionary<string, string>
        _invitationLinks = new(
            StringComparer.OrdinalIgnoreCase);

    public Task SendProjectInvitationAsync(
        string email,
        string inviterName,
        string projectTitle,
        string role,
        string invitationLink,
        CancellationToken cancellationToken = default)
    {
        _invitationLinks[email] = invitationLink;

        return Task.CompletedTask;
    }

    public string GetInvitationToken(string email)
    {
        if (!_invitationLinks.TryGetValue(
                email,
                out var invitationLink))
        {
            throw new InvalidOperationException(
                $"No invitation email was captured for {email}.");
        }

        var uri = new Uri(invitationLink);
        var query = uri.Query.TrimStart('?');

        var tokenPair = query
            .Split(
                '&',
                StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .SingleOrDefault(parts =>
                parts.Length == 2 &&
                string.Equals(
                    parts[0],
                    "token",
                    StringComparison.OrdinalIgnoreCase));

        if (tokenPair is null)
        {
            throw new InvalidOperationException(
                "The captured invitation link did not contain a token.");
        }

        return Uri.UnescapeDataString(tokenPair[1]);
    }
}
