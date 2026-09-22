namespace UniPilot.Application.Auth;

public sealed record RegisterCommand(
    string FullName,
    string Email,
    string Password);