using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.Application.Notifications;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(
    INotificationService notificationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var notifications =
            await notificationService.GetAllAsync(
                userId,
                cancellationToken);

        return Ok(notifications);
    }

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var updated =
            await notificationService.MarkAsReadAsync(
                userId,
                notificationId,
                cancellationToken);

        if (!updated)
        {
            return NotFound(new
            {
                message =
                    "Notification was not found."
            });
        }

        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        await notificationService.MarkAllAsReadAsync(
            userId,
            cancellationToken);

        return NoContent();
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out userId);
    }
}