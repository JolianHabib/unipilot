using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.ProjectMembers;
using UniPilot.Application.ProjectMembers;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/project-invitations")]
public sealed class ProjectInvitationsController(
    IProjectMemberService memberService)
    : ControllerBase
{
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
        AcceptProjectInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new
            {
                message = "Invitation token is required."
            });
        }

        var result = await memberService.AcceptInvitationAsync(
            userId,
            request.Token,
            cancellationToken);

        return result.Status switch
        {
            ProjectInvitationAcceptanceStatus.Success =>
                Ok(new
                {
                    academicProjectId = result.AcademicProjectId
                }),

            ProjectInvitationAcceptanceStatus.Expired =>
                StatusCode(
                    StatusCodes.Status410Gone,
                    new
                    {
                        message = "This invitation has expired."
                    }),

            ProjectInvitationAcceptanceStatus.EmailMismatch =>
                StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "Sign in with the email address that received this invitation."
                    }),

            _ => BadRequest(new
            {
                message = "This invitation link is invalid."
            })
        };
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
