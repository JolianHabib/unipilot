namespace UniPilot.Application.Auth;

public sealed record LoginResult(
    bool Succeeded,
    Guid? UserId,
    string? FullName,
    string? Email,
    string? AccessToken,
    DateTime? ExpiresAtUtc,
    string? Error);