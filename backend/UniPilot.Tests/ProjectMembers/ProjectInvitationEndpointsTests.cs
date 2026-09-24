using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniPilot.Application.ProjectMembers;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.ProjectMembers;

public sealed class ProjectInvitationEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectInvitationEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AcceptInvitation_WithMatchingEmail_Succeeds()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var projectId = await CreateProjectAsync(owner.Token);
        var invitedEmail = CreateEmail("invited");

        await InviteAsync(
            owner.Token,
            projectId,
            invitedEmail);

        var invitationToken =
            GetInvitationToken(invitedEmail);

        var invitedUser = await RegisterAndLoginAsync(
            "Invited User",
            invitedEmail);

        var response = await AcceptInvitationAsync(
            invitedUser.Token,
            invitationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            projectId,
            body.RootElement
                .GetProperty("academicProjectId")
                .GetGuid());

        using var projectsRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/projects",
            invitedUser.Token);

        var projectsResponse =
            await _client.SendAsync(projectsRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            projectsResponse.StatusCode);

        using var projectsBody = JsonDocument.Parse(
            await projectsResponse.Content.ReadAsStringAsync());

        Assert.Contains(
            projectsBody.RootElement.EnumerateArray(),
            project =>
                project.GetProperty("id").GetGuid() ==
                projectId);

        var reusedResponse = await AcceptInvitationAsync(
            invitedUser.Token,
            invitationToken);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            reusedResponse.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_WithDifferentEmail_ReturnsForbidden()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var otherUser = await RegisterAndLoginAsync("Other User");
        var projectId = await CreateProjectAsync(owner.Token);
        var invitedEmail = CreateEmail("expected");

        await InviteAsync(
            owner.Token,
            projectId,
            invitedEmail);

        var invitationToken =
            GetInvitationToken(invitedEmail);

        var response = await AcceptInvitationAsync(
            otherUser.Token,
            invitationToken);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_WhenExpired_ReturnsGone()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var projectId = await CreateProjectAsync(owner.Token);
        var invitedEmail = CreateEmail("expired");

        await InviteAsync(
            owner.Token,
            projectId,
            invitedEmail);

        var invitationToken =
            GetInvitationToken(invitedEmail);

        await ExpireInvitationAsync(invitedEmail);

        var invitedUser = await RegisterAndLoginAsync(
            "Expired Invitee",
            invitedEmail);

        var response = await AcceptInvitationAsync(
            invitedUser.Token,
            invitationToken);

        Assert.Equal(
            HttpStatusCode.Gone,
            response.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_WithInvalidToken_ReturnsBadRequest()
    {
        var user = await RegisterAndLoginAsync("User");

        var response = await AcceptInvitationAsync(
            user.Token,
            Convert.ToHexString(Guid.NewGuid().ToByteArray()));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task InviteAsync(
        string ownerToken,
        Guid projectId,
        string email)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/members",
            ownerToken);

        request.Content = JsonContent.Create(new
        {
            email,
            role = "Viewer"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    private async Task<HttpResponseMessage> AcceptInvitationAsync(
        string token,
        string invitationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/project-invitations/accept",
            token);

        request.Content = JsonContent.Create(new
        {
            token = invitationToken
        });

        return await _client.SendAsync(request);
    }

    private string GetInvitationToken(string email)
    {
        var sender = _factory.Services
            .GetRequiredService<IProjectInvitationEmailSender>();

        var testSender = Assert.IsType<
            TestProjectInvitationEmailSender>(sender);

        return testSender.GetInvitationToken(email);
    }

    private async Task ExpireInvitationAsync(string email)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var invitation = await dbContext.ProjectMembers
            .SingleAsync(member =>
                member.InvitedEmail == email);

        invitation.InvitationExpiresAtUtc =
            DateTime.UtcNow.AddMinutes(-1);

        await dbContext.SaveChangesAsync();
    }

    private async Task<TestUser> RegisterAndLoginAsync(
        string fullName,
        string? email = null)
    {
        email ??= CreateEmail("member");

        const string password = "Test1234!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                fullName,
                email,
                password
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var body = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = body.RootElement
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Login did not return an access token.");

        return new TestUser(email, token);
    }

    private async Task<Guid> CreateProjectAsync(string token)
    {
        var courseId = await CreateCourseAsync(token);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/courses/{courseId}/projects",
            token);

        request.Content = JsonContent.Create(new
        {
            title = "Invitation Test Project",
            description = "Secure invitation test",
            dueDateUtc = "2026-12-20T20:00:00Z"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<Guid> CreateCourseAsync(string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name = $"Invitation Course {Guid.NewGuid():N}",
            code = "INVITE",
            description = "Secure invitation test"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string uri,
        string token)
    {
        var request = new HttpRequestMessage(method, uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    private static string CreateEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }

    private sealed record TestUser(
        string Email,
        string Token);
}
