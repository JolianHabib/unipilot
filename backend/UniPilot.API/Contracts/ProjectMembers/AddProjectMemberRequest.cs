namespace UniPilot.API.Contracts.ProjectMembers;

public sealed class AddProjectMemberRequest
{
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Viewer";
}
