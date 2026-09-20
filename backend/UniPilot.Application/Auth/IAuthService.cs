namespace UniPilot.Application.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<ResetPasswordResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default);
}