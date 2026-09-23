using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.ProjectMembers;

public sealed class ProjectMemberEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectMemberEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddMember_WithRegisteredUser_ReturnsCreated()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var invited = await RegisterAndLoginAsync("Invited Viewer");
        var projectId = await CreateProjectAsync(owner.Token);

        var response = await SendMemberRequestAsync(
            HttpMethod.Post,
            projectId,
            owner.Token,
            new
            {
                email = invited.Email,
                role = "Viewer"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            invited.Email,
            body.RootElement.GetProperty("email").GetString());
        Assert.Equal(
            "Viewer",
            body.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task AddMember_Twice_ReturnsConflict()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var invited = await RegisterAndLoginAsync("Invited Editor");
        var projectId = await CreateProjectAsync(owner.Token);

        var firstResponse = await SendMemberRequestAsync(
            HttpMethod.Post,
            projectId,
            owner.Token,
            new
            {
                email = invited.Email,
                role = "Editor"
            });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await SendMemberRequestAsync(
            HttpMethod.Post,
            projectId,
            owner.Token,
            new
            {
                email = invited.Email,
                role = "Viewer"
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task AddMember_WithUnknownEmail_ReturnsNotFound()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var projectId = await CreateProjectAsync(owner.Token);

        var response = await SendMemberRequestAsync(
            HttpMethod.Post,
            projectId,
            owner.Token,
            new
            {
                email = $"missing-{Guid.NewGuid():N}@example.com",
                role = "Viewer"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndRemoveMember_Succeeds()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var invited = await RegisterAndLoginAsync("Team Member");
        var projectId = await CreateProjectAsync(owner.Token);

        var addResponse = await SendMemberRequestAsync(
            HttpMethod.Post,
            projectId,
            owner.Token,
            new
            {
                email = invited.Email,
                role = "Viewer"
            });

        using var addBody = JsonDocument.Parse(
            await addResponse.Content.ReadAsStringAsync());

        var memberId = addBody.RootElement
            .GetProperty("id")
            .GetGuid();

        using var updateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/projects/{projectId}/members/{memberId}",
            owner.Token);

        updateRequest.Content = JsonContent.Create(new
        {
            role = "Editor"
        });

        var updateResponse = await _client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var updateBody = JsonDocument.Parse(
            await updateResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "Editor",
            updateBody.RootElement.GetProperty("role").GetString());

        using var deleteRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/projects/{projectId}/members/{memberId}",
            owner.Token);

        var deleteResponse = await _client.SendAsync(deleteRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ManageMembers_ByNonOwner_ReturnsNotFound()
    {
        var owner = await RegisterAndLoginAsync("Owner");
        var other = await RegisterAndLoginAsync("Other User");
        var invited = await RegisterAndLoginAsync("Invited User");
        var projectId = await CreateProjectAsync(owner.Token);

        var response = await SendMemberRequestAsync(
            HttpMethod.Post,
            projectId,
            other.Token,
            new
            {
                email = invited.Email,
                role = "Editor"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendMemberRequestAsync(
        HttpMethod method,
        Guid projectId,
        string token,
        object content)
    {
        using var request = CreateAuthorizedRequest(
            method,
            $"/api/projects/{projectId}/members",
            token);

        request.Content = JsonContent.Create(content);

        return await _client.SendAsync(request);
    }

    private async Task<TestUser> RegisterAndLoginAsync(string fullName)
    {
        var email =
            $"member-test-{Guid.NewGuid():N}@example.com";

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

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

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
            title = "Shared Project",
            description = "Project member integration test",
            dueDateUtc = "2026-12-20T20:00:00Z"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateCourseAsync(string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name = $"Shared Course {Guid.NewGuid():N}",
            code = "SHARED",
            description = "Project member integration test"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
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

    private sealed record TestUser(
        string Email,
        string Token);
}
