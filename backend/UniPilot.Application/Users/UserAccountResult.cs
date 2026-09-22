namespace UniPilot.Application.Users;

public sealed record UserAccountResult(
    Guid Id,
    string FullName,
    string Email);