namespace UniPilot.Application.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default);
}