using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gridify;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Dtos.Users;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Controllers;

// User administration. Every action is admin-only.
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController(IUserService users) : ControllerBase
{
    // GET api/users?page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers(
        [FromQuery] GridifyQuery query,
        CancellationToken ct)
        => Ok(await users.GetUsersAsync(query, ct));

    // GET api/users/{id}
    [HttpGet("{id:guid}", Name = nameof(GetUser))]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await users.GetUserAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    // POST api/users
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var result = await users.CreateUserAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        var user = result.Value!;
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    // PUT api/users/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var result = await users.UpdateUserAsync(id, request, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    // DELETE api/users/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await users.DeleteUserAsync(id, currentUserId, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    // PUT api/users/{id}/password
    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetPassword(Guid id, SetPasswordRequest request, CancellationToken ct)
    {
        var result = await users.SetPasswordAsync(id, request.NewPassword, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private ActionResult Failure(Result result) => result.Status switch
    {
        ResultStatus.NotFound => NotFound(),
        ResultStatus.Forbidden => Forbid(),
        ResultStatus.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
        ResultStatus.Invalid => UnprocessableEntity(new ProblemDetails { Detail = result.Error }),
        _ => Problem(),
    };
}
