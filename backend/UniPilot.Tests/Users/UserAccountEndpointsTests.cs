using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Users;

public sealed class UserAccountEndpointsTests
    : IClassFixture<
        CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserAccountEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentUser_WithToken_ReturnsUser()
    {
        var account =
            await RegisterAndLoginAsync();

        UseToken(account.Token);

        var response =
            await _client.GetAsync(
                "/api/users/me");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var body =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            account.FullName,
            body.RootElement
                .GetProperty("fullName")
                .GetString());

        Assert.Equal(
            account.Email,
            body.RootElement
                .GetProperty("email")
                .GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithValidName_UpdatesUser()
    {
        var account =
            await RegisterAndLoginAsync();

        UseToken(account.Token);

        var updateResponse =
            await _client.PutAsJsonAsync(
                "/api/users/me",
                new
                {
                    fullName =
                        "Updated Test User"
                });

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        using var updateBody =
            JsonDocument.Parse(
                await updateResponse.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "Updated Test User",
            updateBody.RootElement
                .GetProperty("fullName")
                .GetString());

        var getResponse =
            await _client.GetAsync(
                "/api/users/me");

        using var getBody =
            JsonDocument.Parse(
                await getResponse.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "Updated Test User",
            getBody.RootElement
                .GetProperty("fullName")
                .GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyName_ReturnsBadRequest()
    {
        var account =
            await RegisterAndLoginAsync();

        UseToken(account.Token);

        var response =
            await _client.PutAsJsonAsync(
                "/api/users/me",
                new
                {
                    fullName = "   "
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidPasswords_AllowsNewPassword()
    {
        var account =
            await RegisterAndLoginAsync();

        UseToken(account.Token);

        const string newPassword =
            "NewPassword123!";

        var changeResponse =
            await _client.PostAsJsonAsync(
                "/api/users/me/password",
                new
                {
                    currentPassword =
                        account.Password,
                    newPassword
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            changeResponse.StatusCode);

        _client.DefaultRequestHeaders
            .Authorization = null;

        var oldPasswordLogin =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email = account.Email,
                    password =
                        account.Password
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldPasswordLogin.StatusCode);

        var newPasswordLogin =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email = account.Email,
                    password = newPassword
                });

        Assert.Equal(
            HttpStatusCode.OK,
            newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ReturnsBadRequest()
    {
        var account =
            await RegisterAndLoginAsync();

        UseToken(account.Token);

        var response =
            await _client.PostAsJsonAsync(
                "/api/users/me/password",
                new
                {
                    currentPassword =
                        "IncorrectPassword123!",
                    newPassword =
                        "NewPassword123!"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_ReturnsBadRequest()
    {
        var account =
            await RegisterAndLoginAsync();

        UseToken(account.Token);

        var response =
            await _client.PostAsJsonAsync(
                "/api/users/me/password",
                new
                {
                    currentPassword =
                        account.Password,
                    newPassword = "123"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotUpdateAnotherUser()
    {
        var firstAccount =
            await RegisterAndLoginAsync(
                "First User");

        var secondAccount =
            await RegisterAndLoginAsync(
                "Second User");

        UseToken(firstAccount.Token);

        var updateResponse =
            await _client.PutAsJsonAsync(
                "/api/users/me",
                new
                {
                    fullName =
                        "Changed First User"
                });

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        UseToken(secondAccount.Token);

        var secondUserResponse =
            await _client.GetAsync(
                "/api/users/me");

        Assert.Equal(
            HttpStatusCode.OK,
            secondUserResponse.StatusCode);

        using var body =
            JsonDocument.Parse(
                await secondUserResponse.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "Second User",
            body.RootElement
                .GetProperty("fullName")
                .GetString());
    }

    private async Task<TestAccount>
        RegisterAndLoginAsync(
            string fullName =
                "Profile Test User")
    {
        var email =
            $"profile-{Guid.NewGuid():N}@example.com";

        const string password =
            "TestPassword123!";

        var registerResponse =
            await _client.PostAsJsonAsync(
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

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var body =
            JsonDocument.Parse(
                await loginResponse.Content
                    .ReadAsStringAsync());

        var token =
            body.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(
                token));

        return new TestAccount(
            fullName,
            email,
            password,
            token!);
    }

    private void UseToken(
        string token)
    {
        _client.DefaultRequestHeaders
            .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
    }

    private sealed record TestAccount(
        string FullName,
        string Email,
        string Password,
        string Token);
}