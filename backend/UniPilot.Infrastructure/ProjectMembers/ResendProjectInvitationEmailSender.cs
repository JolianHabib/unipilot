using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using UniPilot.Application.ProjectMembers;

namespace UniPilot.Infrastructure.ProjectMembers;

public sealed class ResendProjectInvitationEmailSender(
    HttpClient httpClient,
    IConfiguration configuration)
    : IProjectInvitationEmailSender
{
    public async Task SendProjectInvitationAsync(
        string email,
        string inviterName,
        string projectTitle,
        string role,
        string invitationLink,
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

        var safeInviterName =
            WebUtility.HtmlEncode(inviterName);

        var safeProjectTitle =
            WebUtility.HtmlEncode(projectTitle);

        var safeRole =
            WebUtility.HtmlEncode(role);

        var safeInvitationLink =
            WebUtility.HtmlEncode(invitationLink);

        var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta
                name="viewport"
                content="width=device-width, initial-scale=1.0">
            <title>Join a project on UniPilot</title>
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
                    <td align="center" style="padding: 40px 16px;">
                        <table
                            role="presentation"
                            width="100%"
                            cellspacing="0"
                            cellpadding="0"
                            border="0"
                            style="
                                max-width: 600px;
                                background-color: #ffffff;
                                border: 1px solid #e4e6ef;
                                border-radius: 20px;
                                overflow: hidden;
                            "
                        >
                            <tr>
                                <td style="
                                    padding: 28px 36px;
                                    background-color: #5b5ce2;
                                    color: #ffffff;
                                    font-size: 24px;
                                    font-weight: 700;
                                ">
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
                                        letter-spacing: 1px;
                                        text-transform: uppercase;
                                    ">
                                        Project invitation
                                    </p>

                                    <h1 style="
                                        margin: 0 0 20px;
                                        color: #15192d;
                                        font-size: 28px;
                                        line-height: 1.3;
                                    ">
                                        You are invited to collaborate
                                    </h1>

                                    <p style="
                                        margin: 0 0 16px;
                                        color: #555c72;
                                        font-size: 16px;
                                        line-height: 1.7;
                                    ">
                                        {{safeInviterName}} invited you to
                                        join <strong>{{safeProjectTitle}}</strong>
                                        as <strong>{{safeRole}}</strong>.
                                    </p>

                                    <p style="
                                        margin: 0 0 28px;
                                        color: #555c72;
                                        font-size: 16px;
                                        line-height: 1.7;
                                    ">
                                        Accept the invitation to open the
                                        shared project in UniPilot. If you do
                                        not have an account yet, you can create
                                        one using this email address.
                                    </p>

                                    <table
                                        role="presentation"
                                        cellspacing="0"
                                        cellpadding="0"
                                        border="0"
                                    >
                                        <tr>
                                            <td style="
                                                border-radius: 12px;
                                                background-color: #5b5ce2;
                                            ">
                                                <a
                                                    href="{{safeInvitationLink}}"
                                                    style="
                                                        display: inline-block;
                                                        padding: 15px 26px;
                                                        color: #ffffff;
                                                        font-size: 16px;
                                                        font-weight: 700;
                                                        text-decoration: none;
                                                    "
                                                >
                                                    Accept invitation
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
                                        This invitation link expires for your
                                        security. If you were not expecting
                                        this invitation, you can ignore it.
                                    </p>

                                    <p style="
                                        margin: 24px 0 0;
                                        color: #777e91;
                                        font-size: 13px;
                                        line-height: 1.6;
                                    ">
                                        If the button does not work, copy and
                                        paste this link:
                                    </p>

                                    <p style="
                                        margin: 6px 0 0;
                                        font-size: 12px;
                                        line-height: 1.6;
                                        word-break: break-all;
                                    ">
                                        <a
                                            href="{{safeInvitationLink}}"
                                            style="color: #5b5ce2;"
                                        >
                                            {{safeInvitationLink}}
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

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.resend.com/emails");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                apiKey);

        request.Content = JsonContent.Create(new
        {
            from = $"{fromName} <{fromEmail}>",
            to = new[]
            {
                email
            },
            subject = $"Join {projectTitle} on UniPilot",
            html,
            text =
                $"{inviterName} invited you to join " +
                $"{projectTitle} as {role}.\n\n" +
                "Accept the invitation using this link:\n\n" +
                $"{invitationLink}\n\n" +
                "If you were not expecting this invitation, " +
                "you can ignore this email."
        });

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "The project invitation email could not be sent.");
        }
    }
}
