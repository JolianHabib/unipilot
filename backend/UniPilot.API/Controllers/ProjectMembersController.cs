using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.ProjectMembers;
using UniPilot.Application.ProjectMembers;
using UniPilot.Domain.Projects;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/members")]
public sealed class ProjectMembersController(
    IProjectMemberService memberService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var members = await memberService.GetByProjectAsync(
            ownerId,
            projectId,
            cancellationToken);

        return members is null ? NotFound() : Ok(members);
    }

    [HttpPost]
    public async Task<IActionResult> Add(
        Guid projectId,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        if (!TryParseRole(request.Role, out var role))
        {
            return BadRequest(new { message = "Role must be Viewer or Editor." });
        }

        var result = await memberService.AddAsync(
            ownerId,
            projectId,
            request.Email,
            role,
            cancellationToken);

        return result.Status switch
        {
            ProjectMemberOperationStatus.Success =>
                StatusCode(StatusCodes.Status201Created, result.Member),
            ProjectMemberOperationStatus.UserNotFound =>
                NotFound(new { message = "No registered user has this email." }),
            ProjectMemberOperationStatus.OwnerCannotBeMember =>
                Conflict(new { message = "The project owner already has full access." }),
            ProjectMemberOperationStatus.AlreadyMember =>
                Conflict(new { message = "This user is already a project member." }),
            _ => NotFound(new { message = "Project was not found." })
        };
    }

    [HttpPut("{memberId:guid}")]
    public async Task<IActionResult> UpdateRole(
        Guid projectId,
        Guid memberId,
        UpdateProjectMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
        {
            return Unauthorized();
        }

        if (!TryParseRole(request.Role, out var role))
        {
            return BadRequest(new { message = "Role must be Viewer or Editor." });
        }

        var result = await memberService.UpdateRoleAsync(
            ownerId,
            projectId,
            memberId,
            role,
            cancellationToken);

        return result.Status == ProjectMemberOperationStatus.Success
            ? Ok(result.Member)
            : NotFound();
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Remove(
        Guid projectId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var status = await memberService.RemoveAsync(
            ownerId,
            projectId,
            memberId,
            cancellationToken);

        return status == ProjectMemberOperationStatus.Success
            ? NoContent()
            : NotFound();
    }

    private static bool TryParseRole(
        string value,
        out ProjectMemberRole role)
    {
        return Enum.TryParse(value, true, out role) &&
            Enum.IsDefined(role);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
