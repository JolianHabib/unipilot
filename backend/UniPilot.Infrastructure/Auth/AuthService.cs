using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Auth;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Auth;

public sealed class AuthService(AppDbContext dbContext) : IAuthService
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
}