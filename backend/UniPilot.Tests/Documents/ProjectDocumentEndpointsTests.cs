using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Documents;

public sealed class ProjectDocumentEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectDocumentEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ValidPdf_ReturnsCreated()
    {
        var token =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(token);

        using var request =
            CreateUploadRequest(
                token,
                projectId,
                CreatePdfBytes());

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    [Fact]
    public async Task Upload_SamePdfTwice_ReturnsConflict()
    {
        var token =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(token);

        var pdf = CreatePdfBytes();

        using var firstRequest =
            CreateUploadRequest(
                token,
                projectId,
                pdf);

        using var secondRequest =
            CreateUploadRequest(
                token,
                projectId,
                pdf);

        var firstResponse =
            await _client.SendAsync(
                firstRequest);

        var secondResponse =
            await _client.SendAsync(
                secondRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Upload_InvalidPdfSignature_ReturnsBadRequest()
    {
        var token =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(token);

        var invalidContent =
            Encoding.UTF8.GetBytes(
                "This is not a PDF.");

        using var request =
            CreateUploadRequest(
                token,
                projectId,
                invalidContent);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Upload_ToAnotherUsersProject_ReturnsNotFound()
    {
        var ownerToken =
            await RegisterAndLoginAsync();

        var otherToken =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(
                ownerToken);

        using var request =
            CreateUploadRequest(
                otherToken,
                projectId,
                CreatePdfBytes());

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetFile_AsOwner_ReturnsPdf()
    {
        var token =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(token);

        var expectedPdf =
            CreatePdfBytes();

        var documentId =
            await UploadDocumentAsync(
                token,
                projectId,
                expectedPdf);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/projects/{projectId}/documents/{documentId}/file",
                token);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/pdf",
            response.Content.Headers
                .ContentType
                ?.MediaType);

        var returnedPdf =
            await response.Content
                .ReadAsByteArrayAsync();

        Assert.Equal(
            expectedPdf,
            returnedPdf);
    }

    [Fact]
    public async Task GetFile_WithoutToken_ReturnsUnauthorized()
    {
        var ownerToken =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(
                ownerToken);

        var documentId =
            await UploadDocumentAsync(
                ownerToken,
                projectId,
                CreatePdfBytes());

        var response =
            await _client.GetAsync(
                $"/api/projects/{projectId}/documents/{documentId}/file");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetFile_AsAnotherUser_ReturnsNotFound()
    {
        var ownerToken =
            await RegisterAndLoginAsync();

        var otherToken =
            await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(
                ownerToken);

        var documentId =
            await UploadDocumentAsync(
                ownerToken,
                projectId,
                CreatePdfBytes());

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/projects/{projectId}/documents/{documentId}/file",
                otherToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
public async Task RetryProcessing_FailedDocument_ReturnsUnprocessableEntity()
{
    var token =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(token);

    var documentId =
        await UploadDocumentAsync(
            token,
            projectId,
            CreatePdfBytes());

    using var request =
        CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/documents/{documentId}/retry",
            token);

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.UnprocessableEntity,
        response.StatusCode);
}

[Fact]
public async Task RetryProcessing_WithoutToken_ReturnsUnauthorized()
{
    var ownerToken =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(
            ownerToken);

    var documentId =
        await UploadDocumentAsync(
            ownerToken,
            projectId,
            CreatePdfBytes());

    using var request =
        new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/projects/{projectId}/documents/{documentId}/retry");

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);
}

[Fact]
public async Task RetryProcessing_AsAnotherUser_ReturnsNotFound()
{
    var ownerToken =
        await RegisterAndLoginAsync();

    var otherToken =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(
            ownerToken);

    var documentId =
        await UploadDocumentAsync(
            ownerToken,
            projectId,
            CreatePdfBytes());

    using var request =
        CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/documents/{documentId}/retry",
            otherToken);

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.NotFound,
        response.StatusCode);
}
[Fact]
public async Task Upload_CreatesUnreadNotification()
{
    var token =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(token);

    await UploadDocumentAsync(
        token,
        projectId,
        CreatePdfBytes());

    var notifications =
        await GetNotificationsAsync(token);

    Assert.Equal(
        JsonValueKind.Array,
        notifications.ValueKind);

    Assert.NotEqual(
        0,
        notifications.GetArrayLength());

    var notification =
        notifications[0];

    Assert.False(
        notification
            .GetProperty("isRead")
            .GetBoolean());

    Assert.Contains(
        "PDF processing",
        notification
            .GetProperty("title")
            .GetString());
}

[Fact]
public async Task MarkNotificationAsRead_AsOwner_ReturnsNoContent()
{
    var token =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(token);

    await UploadDocumentAsync(
        token,
        projectId,
        CreatePdfBytes());

    var notifications =
        await GetNotificationsAsync(token);

    var notificationId =
        notifications[0]
            .GetProperty("id")
            .GetGuid();

    using var request =
        CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/notifications/{notificationId}/read",
            token);

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.NoContent,
        response.StatusCode);

    var updatedNotifications =
        await GetNotificationsAsync(token);

    var updatedNotification =
        updatedNotifications
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("id")
                    .GetGuid() ==
                notificationId);

    Assert.True(
        updatedNotification
            .GetProperty("isRead")
            .GetBoolean());
}

[Fact]
public async Task MarkNotificationAsRead_AsAnotherUser_ReturnsNotFound()
{
    var ownerToken =
        await RegisterAndLoginAsync();

    var otherToken =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(
            ownerToken);

    await UploadDocumentAsync(
        ownerToken,
        projectId,
        CreatePdfBytes());

    var notifications =
        await GetNotificationsAsync(
            ownerToken);

    var notificationId =
        notifications[0]
            .GetProperty("id")
            .GetGuid();

    using var request =
        CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/notifications/{notificationId}/read",
            otherToken);

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.NotFound,
        response.StatusCode);
}

[Fact]
public async Task MarkAllNotificationsAsRead_MarksEveryNotification()
{
    var token =
        await RegisterAndLoginAsync();

    var projectId =
        await CreateProjectAsync(token);

    await UploadDocumentAsync(
        token,
        projectId,
        CreatePdfBytes());

    using var request =
        CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/notifications/read-all",
            token);

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.NoContent,
        response.StatusCode);

    var notifications =
        await GetNotificationsAsync(token);

    Assert.All(
        notifications.EnumerateArray(),
        notification =>
            Assert.True(
                notification
                    .GetProperty("isRead")
                    .GetBoolean()));
}
private async Task<JsonElement>
    GetNotificationsAsync(
        string token)
{
    using var request =
        CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/notifications",
            token);

    var response =
        await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);

    using var body =
        JsonDocument.Parse(
            await response.Content
                .ReadAsStringAsync());

    return body.RootElement.Clone();
}
    private async Task<Guid> UploadDocumentAsync(
        string token,
        Guid projectId,
        byte[] pdf)
    {
        using var request =
            CreateUploadRequest(
                token,
                projectId,
                pdf);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return body.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<string>
        RegisterAndLoginAsync()
    {
        var email =
            $"document-test-{Guid.NewGuid():N}@example.com";

        const string password =
            "Test1234!";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    fullName =
                        "Document Test User",
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

        return body.RootElement
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Login did not return a token.");
    }

    private async Task<Guid>
        CreateProjectAsync(
            string token)
    {
        var courseId =
            await CreateCourseAsync(token);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/courses/{courseId}/projects",
                token);

        request.Content =
            JsonContent.Create(new
            {
                title =
                    "Document Test Project",
                description =
                    "PDF upload testing",
                dueDateUtc =
                    "2026-12-20T20:00:00Z"
            });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return body.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<Guid>
        CreateCourseAsync(
            string token)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/courses",
                token);

        request.Content =
            JsonContent.Create(new
            {
                name =
                    $"Document Course {Guid.NewGuid():N}",
                code = "DOC",
                description =
                    "Document testing course"
            });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return body.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private static HttpRequestMessage
        CreateUploadRequest(
            string token,
            Guid projectId,
            byte[] fileBytes)
    {
        var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/projects/{projectId}/documents",
                token);

        var form =
            new MultipartFormDataContent();

        var fileContent =
            new ByteArrayContent(
                fileBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/pdf");

        form.Add(
            fileContent,
            "File",
            "requirements.pdf");

        form.Add(
            new StringContent(
                "Specification"),
            "DocumentType");

        request.Content = form;

        return request;
    }

    private static HttpRequestMessage
        CreateAuthorizedRequest(
            HttpMethod method,
            string uri,
            string token)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }

    private static byte[] CreatePdfBytes()
    {
        return Encoding.ASCII.GetBytes(
            "%PDF-1.4\nTest PDF content\n%%EOF");
    }
}