namespace UniPilot.Application.Users;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string FullName);