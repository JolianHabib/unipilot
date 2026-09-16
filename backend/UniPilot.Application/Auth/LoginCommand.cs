namespace UniPilot.Application.Auth;

public sealed record LoginCommand(
    string Email,
    string Password);