namespace UniPilot.Application.Auth;

public sealed record TokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);