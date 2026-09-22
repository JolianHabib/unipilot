using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Users;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Users;

public sealed class UserAccountService(
    AppDbContext dbContext)
    : IUserAccountService
{
    public async Task<UserAccountResult?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId)
            .Select(user =>
                new UserAccountResult(
                    user.Id,
                    user.FullName,
                    user.Email))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<UserAccountResult?>
        UpdateProfileAsync(
            UpdateProfileCommand command,
            CancellationToken cancellationToken = default)
    {
        var user =
            await dbContext.Users
                .SingleOrDefaultAsync(
                    existingUser =>
                        existingUser.Id ==
                        command.UserId,
                    cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.FullName =
            command.FullName.Trim();

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new UserAccountResult(
            user.Id,
            user.FullName,
            user.Email);
    }

    public async Task<ChangePasswordResult>
        ChangePasswordAsync(
            ChangePasswordCommand command,
            CancellationToken cancellationToken = default)
    {
        var user =
            await dbContext.Users
                .SingleOrDefaultAsync(
                    existingUser =>
                        existingUser.Id ==
                        command.UserId,
                    cancellationToken);

        if (user is null)
        {
            return ChangePasswordResult
                .UserNotFound;
        }

        var currentPasswordIsValid =
            BCrypt.Net.BCrypt.Verify(
                command.CurrentPassword,
                user.PasswordHash);

        if (!currentPasswordIsValid)
        {
            return ChangePasswordResult
                .InvalidCurrentPassword;
        }

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                command.NewPassword);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ChangePasswordResult.Success;
    }
}