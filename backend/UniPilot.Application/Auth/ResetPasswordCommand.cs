namespace UniPilot.Application.Auth;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword);