using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Courses;

public sealed class CourseEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CourseEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCourse_WithToken_ReturnsCreated()
    {
        var token = await RegisterAndLoginAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name = "Secure Development",
            code = "SEC-2026",
            description = "Security course"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Secure Development",
            body.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetCourses_ReturnsOnlyCurrentUserCourses()
    {
        var firstUserToken = await RegisterAndLoginAsync();
        var secondUserToken = await RegisterAndLoginAsync();

        await CreateCourseAsync(
            firstUserToken,
            "First User Course");

        await CreateCourseAsync(
            secondUserToken,
            "Second User Course");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/courses",
            firstUserToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var courses = body.RootElement;

        Assert.Contains(
            courses.EnumerateArray(),
            course => course.GetProperty("name").GetString()
                == "First User Course");

        Assert.DoesNotContain(
            courses.EnumerateArray(),
            course => course.GetProperty("name").GetString()
                == "Second User Course");
    }

    [Fact]
    public async Task GetCourses_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/courses");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"course-test-{Guid.NewGuid():N}@example.com";
        const string password = "Test1234!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                fullName = "Course Test User",
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

        return body.RootElement
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Login did not return an access token.");
    }

    private async Task CreateCourseAsync(
        string token,
        string name)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name,
            code = "TEST",
            description = "Integration test course"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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