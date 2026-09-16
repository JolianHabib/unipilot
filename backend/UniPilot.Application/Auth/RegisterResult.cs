namespace UniPilot.Application.Auth;

public sealed record RegisterResult(
    bool Succeeded,
    Guid? UserId,
    string? FullName,
    string? Email,
    string? Error);