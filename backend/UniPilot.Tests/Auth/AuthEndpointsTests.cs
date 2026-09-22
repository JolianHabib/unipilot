using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Auth;

public sealed class AuthEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var request = CreateRegisterRequest();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            request.email,
            body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var request = CreateRegisterRequest();

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        var request = new
        {
            fullName = "Test User",
            email = CreateUniqueEmail(),
            password = "123"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var registration = CreateRegisterRequest();

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            registration);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                registration.email,
                registration.password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var body = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = body.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var protectedResponse = await _client.GetAsync(
            "/api/users/me");

        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var registration = CreateRegisterRequest();

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            registration);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                registration.email,
                password = "WrongPassword!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(
            "/api/users/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static RegisterTestRequest CreateRegisterRequest()
    {
        return new RegisterTestRequest(
            "Integration Test User",
            CreateUniqueEmail(),
            "Test1234!");
    }

    private static string CreateUniqueEmail()
    {
        return $"test-{Guid.NewGuid():N}@example.com";
    }

    private sealed record RegisterTestRequest(
        string fullName,
        string email,
        string password);
}