namespace UniPilot.Application.Users;

public interface IUserAccountService
{
    Task<UserAccountResult?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserAccountResult?> UpdateProfileAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<ChangePasswordResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default);
}