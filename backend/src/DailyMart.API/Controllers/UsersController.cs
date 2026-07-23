using System.Security.Claims;
using DailyMart.Application.Auth;
using DailyMart.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Admin-only - see RolesController's doc comment for why. Distinct from AuthController, which is
/// about the caller's own session, not managing other accounts.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<UserDto>> Update(long id, UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _userService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.DeleteAsync(id, currentUserId, cancellationToken);
        return NoContent();
    }
}
