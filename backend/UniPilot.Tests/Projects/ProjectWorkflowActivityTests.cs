using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Projects;

public sealed class ProjectWorkflowActivityTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectWorkflowActivityTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateManualRequirement_InOwnedProject_ReturnsCreated()
    {
        var token = await RegisterAndLoginAsync();
        var (_, projectId) = await CreateProjectAsync(token);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/requirements",
            token);

        request.Content = CreateRequirementContent(
            "Manual security requirement");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Manual security requirement",
            body.RootElement.GetProperty("title").GetString());

        Assert.Equal(
            JsonValueKind.Null,
            body.RootElement.GetProperty("projectDocumentId").ValueKind);

        Assert.Equal(
            JsonValueKind.Null,
            body.RootElement.GetProperty("sourcePageNumber").ValueKind);
    }

    [Fact]
    public async Task CreateManualRequirement_InAnotherUsersProject_ReturnsNotFound()
    {
        var ownerToken = await RegisterAndLoginAsync();
        var otherToken = await RegisterAndLoginAsync();
        var (_, projectId) = await CreateProjectAsync(ownerToken);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/requirements",
            otherToken);

        request.Content = CreateRequirementContent(
            "Unauthorized requirement");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRequirement_RecordsActivity()
    {
        var token = await RegisterAndLoginAsync();
        var (_, projectId) = await CreateProjectAsync(token);
        var requirementId = await CreateRequirementAsync(
            token,
            projectId,
            "Original requirement");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/requirements/{requirementId}",
            token);

        request.Content = JsonContent.Create(new
        {
            title = "Updated requirement",
            description = "Updated requirement description.",
            type = "NonFunctional",
            priority = "Critical"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var activities = await GetActivitiesAsync(token, projectId);

        Assert.Contains(
            activities.EnumerateArray(),
            activity => activity.GetProperty("type").GetString() ==
                        "RequirementUpdated");
    }

    [Fact]
    public async Task DeleteRequirement_RecordsActivity()
    {
        var token = await RegisterAndLoginAsync();
        var (_, projectId) = await CreateProjectAsync(token);
        var requirementId = await CreateRequirementAsync(
            token,
            projectId,
            "Requirement to delete");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/requirements/{requirementId}",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var activities = await GetActivitiesAsync(token, projectId);

        Assert.Contains(
            activities.EnumerateArray(),
            activity => activity.GetProperty("type").GetString() ==
                        "RequirementDeleted");
    }

    [Fact]
    public async Task ChangeProjectStatus_RecordsActivity()
    {
        var token = await RegisterAndLoginAsync();
        var (courseId, projectId) = await CreateProjectAsync(token);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/courses/{courseId}/projects/{projectId}",
            token);

        request.Content = JsonContent.Create(new
        {
            title = "Workflow Test Project",
            description = "Updated project status test.",
            dueDateUtc = "2026-12-20T20:00:00Z",
            status = "Active"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var activities = await GetActivitiesAsync(token, projectId);

        Assert.Contains(
            activities.EnumerateArray(),
            activity => activity.GetProperty("type").GetString() ==
                        "ProjectStatusChanged");
    }

    private async Task<Guid> CreateRequirementAsync(
        string token,
        Guid projectId,
        string title)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/requirements",
            token);

        request.Content = CreateRequirementContent(title);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> GetActivitiesAsync(
        string token,
        Guid projectId)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/projects/{projectId}/activities",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement.Clone();
    }

    private async Task<(Guid CourseId, Guid ProjectId)>
        CreateProjectAsync(string token)
    {
        var courseId = await CreateCourseAsync(token);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/courses/{courseId}/projects",
            token);

        request.Content = JsonContent.Create(new
        {
            title = "Workflow Test Project",
            description = "Project workflow integration tests.",
            dueDateUtc = "2026-12-20T20:00:00Z"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return (
            courseId,
            body.RootElement.GetProperty("id").GetGuid());
    }

    private async Task<Guid> CreateCourseAsync(string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name = $"Workflow Course {Guid.NewGuid():N}",
            code = "FLOW",
            description = "Workflow integration tests."
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email =
            $"workflow-test-{Guid.NewGuid():N}@example.com";
        const string password = "Test1234!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                fullName = "Workflow Test User",
                email,
                password
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var body = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        return body.RootElement
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Login did not return an access token.");
    }

    private static JsonContent CreateRequirementContent(
        string title)
    {
        return JsonContent.Create(new
        {
            title,
            description = "Manually created requirement description.",
            type = "Functional",
            priority = "High"
        });
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
}
