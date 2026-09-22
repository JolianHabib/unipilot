using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using UniPilot.Application.Auth;

namespace UniPilot.Infrastructure.Auth;

public sealed class ResendPasswordResetEmailSender(
    HttpClient httpClient,
    IConfiguration configuration)
    : IPasswordResetEmailSender
{
    public async Task SendPasswordResetAsync(
        string email,
        string fullName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var apiKey =
            configuration["Resend:ApiKey"]
            ?? throw new InvalidOperationException(
                "Resend API key was not configured.");

        var fromEmail =
            configuration["Resend:FromEmail"]
            ?? throw new InvalidOperationException(
                "Resend sender email was not configured.");

        var fromName =
            configuration["Resend:FromName"]
            ?? "UniPilot";

        var safeName =
            WebUtility.HtmlEncode(fullName);

        var safeResetLink =
            WebUtility.HtmlEncode(resetLink);

        var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta
                name="viewport"
                content="width=device-width, initial-scale=1.0">
            <title>Reset your UniPilot password</title>
        </head>

        <body style="
            margin: 0;
            padding: 0;
            background-color: #f4f5fa;
            font-family: Arial, Helvetica, sans-serif;
            color: #171b2f;
        ">
            <table
                role="presentation"
                width="100%"
                cellspacing="0"
                cellpadding="0"
                border="0"
                style="background-color: #f4f5fa;"
            >
                <tr>
                    <td
                        align="center"
                        style="padding: 40px 16px;"
                    >
                        <table
                            role="presentation"
                            width="100%"
                            cellspacing="0"
                            cellpadding="0"
                            border="0"
                            style="
                                max-width: 600px;
                                background-color: #ffffff;
                                border-radius: 20px;
                                overflow: hidden;
                                border: 1px solid #e4e6ef;
                            "
                        >
                            <tr>
                                <td
                                    style="
                                        padding: 28px 36px;
                                        background-color: #5b5ce2;
                                        color: #ffffff;
                                        font-size: 24px;
                                        font-weight: 700;
                                    "
                                >
                                    UniPilot
                                </td>
                            </tr>

                            <tr>
                                <td style="padding: 40px 36px;">
                                    <p style="
                                        margin: 0 0 12px;
                                        color: #666bdc;
                                        font-size: 13px;
                                        font-weight: 700;
                                        text-transform: uppercase;
                                        letter-spacing: 1px;
                                    ">
                                        Password recovery
                                    </p>

                                    <h1 style="
                                        margin: 0 0 20px;
                                        color: #15192d;
                                        font-size: 28px;
                                        line-height: 1.3;
                                    ">
                                        Reset your password
                                    </h1>

                                    <p style="
                                        margin: 0 0 16px;
                                        color: #555c72;
                                        font-size: 16px;
                                        line-height: 1.7;
                                    ">
                                        Hello {{safeName}},
                                    </p>

                                    <p style="
                                        margin: 0 0 28px;
                                        color: #555c72;
                                        font-size: 16px;
                                        line-height: 1.7;
                                    ">
                                        We received a request to reset
                                        the password for your UniPilot
                                        account. Click the button below
                                        to create a new password.
                                    </p>

                                    <table
                                        role="presentation"
                                        cellspacing="0"
                                        cellpadding="0"
                                        border="0"
                                    >
                                        <tr>
                                            <td
                                                style="
                                                    border-radius: 12px;
                                                    background-color: #5b5ce2;
                                                "
                                            >
                                                <a
                                                    href="{{safeResetLink}}"
                                                    style="
                                                        display: inline-block;
                                                        padding: 15px 26px;
                                                        color: #ffffff;
                                                        font-size: 16px;
                                                        font-weight: 700;
                                                        text-decoration: none;
                                                    "
                                                >
                                                    Reset password
                                                </a>
                                            </td>
                                        </tr>
                                    </table>

                                    <p style="
                                        margin: 28px 0 8px;
                                        color: #777e91;
                                        font-size: 14px;
                                        line-height: 1.6;
                                    ">
                                        This link expires shortly for
                                        your security. If you did not
                                        request a password reset, you
                                        can safely ignore this email.
                                    </p>

                                    <p style="
                                        margin: 24px 0 0;
                                        color: #777e91;
                                        font-size: 13px;
                                        line-height: 1.6;
                                    ">
                                        If the button does not work,
                                        copy and paste this link:
                                    </p>

                                    <p style="
                                        margin: 6px 0 0;
                                        font-size: 12px;
                                        line-height: 1.6;
                                        word-break: break-all;
                                    ">
                                        <a
                                            href="{{safeResetLink}}"
                                            style="color: #5b5ce2;"
                                        >
                                            {{safeResetLink}}
                                        </a>
                                    </p>
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding: 22px 36px;
                                    background-color: #f8f8fc;
                                    color: #8a90a3;
                                    font-size: 12px;
                                    text-align: center;
                                ">
                                    UniPilot · AI-powered academic workspace
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.resend.com/emails");

        request.Headers.Authorization =
            new System.Net.Http.Headers
                .AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

        request.Content = JsonContent.Create(new
        {
            from = $"{fromName} <{fromEmail}>",
            to = new[]
            {
                email
            },
            subject =
                "Reset your UniPilot password",
            html,
            text =
                $"Hello {fullName},\n\n" +
                "Use the following link to reset " +
                "your UniPilot password:\n\n" +
                $"{resetLink}\n\n" +
                "If you did not request this, " +
                "you can ignore this email."
        });

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "The password reset email could not be sent.");
        }
    }
}