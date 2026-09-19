using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Courses;
using UniPilot.Application.Courses;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/courses")]
public sealed class CoursesController(
    ICourseService courseService)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.Name))
        {
            return BadRequest(new
            {
                message =
                    "Course name is required."
            });
        }

        var command =
            new CreateCourseCommand(
                userId,
                request.Name,
                request.Code,
                request.Description);

        var course =
            await courseService.CreateAsync(
                command,
                cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            course);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var courses =
            await courseService
                .GetByOwnerAsync(
                    userId,
                    cancellationToken);

        return Ok(courses);
    }

    [HttpPut("{courseId:guid}")]
    public async Task<IActionResult> Update(
        Guid courseId,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.Name))
        {
            return BadRequest(new
            {
                message =
                    "Course name is required."
            });
        }

        var command =
            new UpdateCourseCommand(
                userId,
                courseId,
                request.Name,
                request.Code,
                request.Description);

        var course =
            await courseService.UpdateAsync(
                command,
                cancellationToken);

        if (course is null)
        {
            return NotFound(new
            {
                message =
                    "Course was not found."
            });
        }

        return Ok(course);
    }

    [HttpDelete("{courseId:guid}")]
    public async Task<IActionResult> Delete(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var deleted =
            await courseService.DeleteAsync(
                userId,
                courseId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message =
                    "Course was not found."
            });
        }

        return NoContent();
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var value =
            User.FindFirst(
                ClaimTypes.NameIdentifier)
                ?.Value;

        return Guid.TryParse(
            value,
            out userId);
    }
}