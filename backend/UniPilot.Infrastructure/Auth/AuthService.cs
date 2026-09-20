using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UniPilot.Application.Auth;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Auth;

public sealed class AuthService(
    AppDbContext dbContext,
    ITokenService tokenService,
    IPasswordResetEmailSender
        passwordResetEmailSender,
    IConfiguration configuration)
    : IAuthService
{
    public async Task<RegisterResult>
        RegisterAsync(
            RegisterCommand command,
            CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            command.Email
                .Trim()
                .ToLowerInvariant();

        var emailExists =
            await dbContext.Users.AnyAsync(
                user =>
                    user.Email ==
                    normalizedEmail,
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
            FullName =
                command.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        command.Password)
        };

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new RegisterResult(
            true,
            user.Id,
            user.FullName,
            user.Email,
            null);
    }

    public async Task<LoginResult>
        LoginAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            command.Email
                .Trim()
                .ToLowerInvariant();

        var user =
            await dbContext.Users
                .SingleOrDefaultAsync(
                    existingUser =>
                        existingUser.Email ==
                        normalizedEmail,
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

        var token =
            tokenService.CreateToken(
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

    public async Task
        ForgotPasswordAsync(
            ForgotPasswordCommand command,
            CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            command.Email
                .Trim()
                .ToLowerInvariant();

        var user =
            await dbContext.Users
                .SingleOrDefaultAsync(
                    existingUser =>
                        existingUser.Email ==
                        normalizedEmail,
                    cancellationToken);

        // Always return normally so callers cannot
        // discover whether an email is registered.
        if (user is null)
        {
            return;
        }

        var resetToken =
            Convert.ToHexString(
                    RandomNumberGenerator
                        .GetBytes(32))
                .ToLowerInvariant();

        user.PasswordResetTokenHash =
            HashToken(resetToken);

        user.PasswordResetTokenExpiresAtUtc =
            DateTime.UtcNow.AddMinutes(
                GetTokenLifetimeMinutes());

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var frontendBaseUrl =
            configuration[
                "Frontend:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Frontend base URL was not configured.");

        var resetLink =
            $"{frontendBaseUrl.TrimEnd('/')}" +
            "/reset-password" +
            $"?email={Uri.EscapeDataString(user.Email)}" +
            $"&token={Uri.EscapeDataString(resetToken)}";

        await passwordResetEmailSender
            .SendPasswordResetAsync(
                user.Email,
                user.FullName,
                resetLink,
                cancellationToken);
    }

    public async Task<ResetPasswordResult>
        ResetPasswordAsync(
            ResetPasswordCommand command,
            CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            command.Email
                .Trim()
                .ToLowerInvariant();

        var user =
            await dbContext.Users
                .SingleOrDefaultAsync(
                    existingUser =>
                        existingUser.Email ==
                        normalizedEmail,
                    cancellationToken);

        if (user is null ||
            string.IsNullOrWhiteSpace(
                user.PasswordResetTokenHash) ||
            user.PasswordResetTokenExpiresAtUtc
                is null ||
            user.PasswordResetTokenExpiresAtUtc <=
                DateTime.UtcNow)
        {
            return ResetPasswordResult
                .InvalidOrExpiredToken;
        }

        var providedTokenHash =
            HashToken(command.Token);

        if (!SecureEquals(
                user.PasswordResetTokenHash,
                providedTokenHash))
        {
            return ResetPasswordResult
                .InvalidOrExpiredToken;
        }

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                command.NewPassword);

        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc =
            null;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ResetPasswordResult.Success;
    }

    private int GetTokenLifetimeMinutes()
    {
        var configuredValue =
            configuration[
                "PasswordReset:TokenLifetimeMinutes"];

        if (!int.TryParse(
                configuredValue,
                out var minutes))
        {
            return 30;
        }

        return Math.Clamp(
            minutes,
            5,
            120);
    }

    private static string HashToken(
        string token)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    token));

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant();
    }

    private static bool SecureEquals(
        string expectedHash,
        string providedHash)
    {
        var expectedBytes =
            Convert.FromHexString(
                expectedHash);

        var providedBytes =
            Convert.FromHexString(
                providedHash);

        return CryptographicOperations
            .FixedTimeEquals(
                expectedBytes,
                providedBytes);
    }
}