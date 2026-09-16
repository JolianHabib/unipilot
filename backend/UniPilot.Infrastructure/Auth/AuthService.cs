using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Auth;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Auth;

public sealed class AuthService(AppDbContext dbContext,ITokenService tokenService) : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var emailExists = await dbContext.Users.AnyAsync(
            user => user.Email == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            return new RegisterResult(
                false,
                null,
                null,
                null,
                "An account with this email already exists.");
        }

        var user = new User
        {
            FullName = command.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResult(
            true,
            user.Id,
            user.FullName,
            user.Email,
            null);
    }
    public async Task<LoginResult> LoginAsync(
    LoginCommand command,
    CancellationToken cancellationToken = default)
{
    var normalizedEmail = command.Email.Trim().ToLowerInvariant();

    var user = await dbContext.Users.SingleOrDefaultAsync(
        existingUser => existingUser.Email == normalizedEmail,
        cancellationToken);

    if (user is null ||
        !BCrypt.Net.BCrypt.Verify(
            command.Password,
            user.PasswordHash))
    {
        return new LoginResult(
            false,
            null,
            null,
            null,
            null,
            null,
            "Invalid email or password.");
    }

    var token = tokenService.CreateToken(
        user.Id,
        user.FullName,
        user.Email);

    return new LoginResult(
        true,
        user.Id,
        user.FullName,
        user.Email,
        token.AccessToken,
        token.ExpiresAtUtc,
        null);
}
}