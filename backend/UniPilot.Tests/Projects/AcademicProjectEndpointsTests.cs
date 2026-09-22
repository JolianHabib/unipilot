using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Projects;

public sealed class AcademicProjectEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AcademicProjectEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProject_InOwnedCourse_ReturnsCreated()
    {
        var token = await RegisterAndLoginAsync();
        var courseId = await CreateCourseAsync(token);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/courses/{courseId}/projects",
            token);

        request.Content = CreateProjectContent(
            "Secure Web Application");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Draft",
            body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetProjects_ReturnsOwnedCourseProjects()
    {
        var token = await RegisterAndLoginAsync();
        var courseId = await CreateCourseAsync(token);

        await CreateProjectAsync(
            token,
            courseId,
            "Requirements Analyzer");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/courses/{courseId}/projects",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Contains(
            body.RootElement.EnumerateArray(),
            project => project
                .GetProperty("title")
                .GetString() == "Requirements Analyzer");
    }

    [Fact]
    public async Task CreateProject_InAnotherUsersCourse_ReturnsNotFound()
    {
        var ownerToken = await RegisterAndLoginAsync();
        var otherUserToken = await RegisterAndLoginAsync();

        var ownerCourseId = await CreateCourseAsync(ownerToken);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/courses/{ownerCourseId}/projects",
            otherUserToken);

        request.Content = CreateProjectContent(
            "Unauthorized Project");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            $"/api/courses/{Guid.NewGuid()}/projects");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email =
            $"project-test-{Guid.NewGuid():N}@example.com";

        const string password = "Test1234!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                fullName = "Project Test User",
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

        return body.RootElement
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Login did not return an access token.");
    }

    private async Task<Guid> CreateCourseAsync(string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name = $"Course {Guid.NewGuid():N}",
            code = "TEST",
            description = "Integration test course"
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

    private async Task CreateProjectAsync(
        string token,
        Guid courseId,
        string title)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/courses/{courseId}/projects",
            token);

        request.Content = CreateProjectContent(title);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    private static JsonContent CreateProjectContent(
        string title)
    {
        return JsonContent.Create(new
        {
            title,
            description = "Integration test project",
            dueDateUtc = "2026-12-20T20:00:00Z"
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