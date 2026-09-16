using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Requirements;

public sealed class ProjectRequirementEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectRequirementEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Extract_ReadyDocument_ReturnsRequirements()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);
        var documentId =
            await UploadDocumentAsync(token, projectId);

        await MarkDocumentReadyAsync(documentId);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/documents/{documentId}/requirements/extract",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var requirements = body.RootElement;

        Assert.Equal(
            JsonValueKind.Array,
            requirements.ValueKind);

        Assert.Single(requirements.EnumerateArray());

        var requirement = requirements[0];

        Assert.Equal(
            "Implement authentication",
            requirement.GetProperty("title").GetString());

        Assert.Equal(
            "Functional",
            requirement.GetProperty("type").GetString());

        Assert.Equal(
            "High",
            requirement.GetProperty("priority").GetString());

        Assert.Equal(
            1,
            requirement
                .GetProperty("sourcePageNumber")
                .GetInt32());
    }

    [Fact]
    public async Task GetByProject_ReturnsSavedRequirements()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);
        var documentId =
            await UploadDocumentAsync(token, projectId);

        await MarkDocumentReadyAsync(documentId);

        using var extractRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/documents/{documentId}/requirements/extract",
            token);

        var extractResponse =
            await _client.SendAsync(extractRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            extractResponse.StatusCode);

        using var getRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/projects/{projectId}/requirements",
            token);

        var getResponse = await _client.SendAsync(getRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using var body = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        Assert.Single(body.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Extract_AnotherUsersDocument_ReturnsNotFound()
    {
        var ownerToken = await RegisterAndLoginAsync();
        var otherToken = await RegisterAndLoginAsync();

        var projectId =
            await CreateProjectAsync(ownerToken);

        var documentId =
            await UploadDocumentAsync(
                ownerToken,
                projectId);

        await MarkDocumentReadyAsync(documentId);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/documents/{documentId}/requirements/extract",
            otherToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task MarkDocumentReadyAsync(
        Guid documentId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var document =
            await dbContext.ProjectDocuments.FindAsync(
                documentId)
            ?? throw new InvalidOperationException(
                "The uploaded document was not found.");

        document.ProcessingStatus =
            DocumentProcessingStatus.Ready;

        document.PageCount = 1;
        document.FailureReason = null;

        dbContext.DocumentPages.Add(
            new DocumentPage
            {
                ProjectDocumentId = documentId,
                PageNumber = 1,
                Text =
                    "The student must implement " +
                    "user authentication."
            });

        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> UploadDocumentAsync(
        string token,
        Guid projectId)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/documents",
            token);

        var form = new MultipartFormDataContent();

        var fileBytes = Encoding.ASCII.GetBytes(
            $"%PDF-1.4\n{Guid.NewGuid():N}\n%%EOF");

        var fileContent =
            new ByteArrayContent(fileBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/pdf");

        form.Add(
            fileContent,
            "File",
            "requirements.pdf");

        form.Add(
            new StringContent("Specification"),
            "DocumentType");

        request.Content = form;

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

    private async Task<string> RegisterAndLoginAsync()
    {
        var email =
            $"requirement-test-{Guid.NewGuid():N}@example.com";

        const string password = "Test1234!";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    fullName = "Requirement Test User",
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

        using var body = JsonDocument.Parse(
            await loginResponse.Content
                .ReadAsStringAsync());

        return body.RootElement
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Login did not return a token.");
    }

    private async Task<Guid> CreateProjectAsync(
        string token)
    {
        var courseId = await CreateCourseAsync(token);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/courses/{courseId}/projects",
            token);

        request.Content = JsonContent.Create(
            new
            {
                title = "Requirement Test Project",
                description =
                    "AI requirement extraction testing",

                dueDateUtc =
                    "2026-12-20T20:00:00Z"
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

    private async Task<Guid> CreateCourseAsync(
        string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(
            new
            {
                name =
                    $"Requirement Course {Guid.NewGuid():N}",

                code = "REQ",
                description =
                    "Requirement testing course"
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

    private static HttpRequestMessage
        CreateAuthorizedRequest(
            HttpMethod method,
            string uri,
            string token)
    {
        var request =
            new HttpRequestMessage(method, uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }
}