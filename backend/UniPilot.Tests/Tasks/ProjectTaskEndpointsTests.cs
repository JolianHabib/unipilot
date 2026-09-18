using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using UniPilot.Domain.Entities;
using UniPilot.Domain.Requirements;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Tests.Infrastructure;

namespace UniPilot.Tests.Tasks;

public sealed class ProjectTaskEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectTaskEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ValidTask_ReturnsCreatedTask()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);

        var response = await CreateTaskResponseAsync(
            token,
            projectId,
            null,
            "Build login page");

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Build login page",
            body.RootElement
                .GetProperty("title")
                .GetString());

        Assert.Equal(
            "ToDo",
            body.RootElement
                .GetProperty("status")
                .GetString());

        Assert.Equal(
            "High",
            body.RootElement
                .GetProperty("priority")
                .GetString());
    }

    [Fact]
    public async Task GetByProject_ReturnsCreatedTasks()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);

        using var createResponse =
            await CreateTaskResponseAsync(
                token,
                projectId,
                null,
                "Test authentication");

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/projects/{projectId}/tasks",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var tasks = body.RootElement.EnumerateArray().ToList();

        Assert.Contains(
            tasks,
            task =>
                task.GetProperty("title").GetString() ==
                "Test authentication");
    }

    [Fact]
    public async Task Update_OwnerCanEditTask()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);
        var taskId = await CreateTaskAsync(
            token,
            projectId,
            null,
            "Original title");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/projects/{projectId}/tasks/{taskId}",
            token);

        request.Content = JsonContent.Create(new
        {
            projectRequirementId = (Guid?)null,
            title = "Updated title",
            description = "Updated description",
            priority = "Medium",
            dueDateUtc = "2026-12-21T10:00:00Z"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Updated title",
            body.RootElement
                .GetProperty("title")
                .GetString());

        Assert.Equal(
            "Medium",
            body.RootElement
                .GetProperty("priority")
                .GetString());
    }

    [Fact]
    public async Task Move_LinkedTaskToDone_CompletesRequirement()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);
        var requirementId =
            await CreateRequirementAsync(projectId);

        var taskId = await CreateTaskAsync(
            token,
            projectId,
            requirementId,
            "Implement authentication");

        using var moveRequest = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/projects/{projectId}/tasks/{taskId}/move",
            token);

        moveRequest.Content = JsonContent.Create(new
        {
            status = "Done",
            position = 0
        });

        var moveResponse =
            await _client.SendAsync(moveRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            moveResponse.StatusCode);

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

        var requirement = body.RootElement
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("id").GetGuid() ==
                requirementId);

        Assert.True(
            requirement
                .GetProperty("isCompleted")
                .GetBoolean());
    }

    [Fact]
    public async Task Move_LinkedTaskOutOfDone_ReopensRequirement()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);
        var requirementId =
            await CreateRequirementAsync(projectId);

        var taskId = await CreateTaskAsync(
            token,
            projectId,
            requirementId,
            "Linked task");

        await MoveTaskAsync(
            token,
            projectId,
            taskId,
            "Done");

        await MoveTaskAsync(
            token,
            projectId,
            taskId,
            "InProgress");

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var requirement = await dbContext
            .ProjectRequirements.FindAsync(requirementId);

        Assert.NotNull(requirement);
        Assert.False(requirement.IsCompleted);
    }

    [Fact]
    public async Task Delete_OwnerCanDeleteTask()
    {
        var token = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(token);
        var taskId = await CreateTaskAsync(
            token,
            projectId,
            null,
            "Temporary task");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/projects/{projectId}/tasks/{taskId}",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        using var secondRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/projects/{projectId}/tasks/{taskId}",
            token);

        var secondResponse =
            await _client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetByProject_AnotherUserReturnsNotFound()
    {
        var ownerToken = await RegisterAndLoginAsync();
        var otherToken = await RegisterAndLoginAsync();
        var projectId =
            await CreateProjectAsync(ownerToken);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/projects/{projectId}/tasks",
            otherToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task<HttpResponseMessage>
        CreateTaskResponseAsync(
            string token,
            Guid projectId,
            Guid? requirementId,
            string title)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/projects/{projectId}/tasks",
            token);

        request.Content = JsonContent.Create(new
        {
            projectRequirementId = requirementId,
            title,
            description = "Task endpoint test",
            priority = "High",
            dueDateUtc = "2026-12-20T20:00:00Z"
        });

        return await _client.SendAsync(request);
    }

    private async Task<Guid> CreateTaskAsync(
        string token,
        Guid projectId,
        Guid? requirementId,
        string title)
    {
        using var response = await CreateTaskResponseAsync(
            token,
            projectId,
            requirementId,
            title);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return body.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private async Task MoveTaskAsync(
        string token,
        Guid projectId,
        Guid taskId,
        string status)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/projects/{projectId}/tasks/{taskId}/move",
            token);

        request.Content = JsonContent.Create(new
        {
            status,
            position = 0
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<Guid> CreateRequirementAsync(
        Guid projectId)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var requirement = new ProjectRequirement
        {
            AcademicProjectId = projectId,
            Title = "Implement authentication",
            Description =
                "The system must authenticate users.",
            Type = RequirementType.Functional,
            Priority = RequirementPriority.High,
            IsCompleted = false
        };

        dbContext.ProjectRequirements.Add(requirement);
        await dbContext.SaveChangesAsync();

        return requirement.Id;
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email =
            $"task-test-{Guid.NewGuid():N}@example.com";

        const string password = "Test1234!";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    fullName = "Task Test User",
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

        request.Content = JsonContent.Create(new
        {
            title = "Task Test Project",
            description = "Project task testing",
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

    private async Task<Guid> CreateCourseAsync(
        string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/courses",
            token);

        request.Content = JsonContent.Create(new
        {
            name = $"Task Course {Guid.NewGuid():N}",
            code = "TASK",
            description = "Task testing course"
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
        var request = new HttpRequestMessage(method, uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }
}
