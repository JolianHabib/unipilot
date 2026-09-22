namespace UniPilot.Application.Auth;

public sealed record ForgotPasswordCommand(
    string Email);