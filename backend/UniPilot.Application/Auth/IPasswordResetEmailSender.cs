namespace UniPilot.Application.Auth;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetAsync(
        string email,
        string fullName,
        string resetLink,
        CancellationToken cancellationToken = default);
}