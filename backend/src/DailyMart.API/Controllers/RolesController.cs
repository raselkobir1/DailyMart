using DailyMart.Application.Common.Models;
using DailyMart.Application.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Admin-only - unlike every business controller in this codebase (which just needs the global
/// "any authenticated user" fallback policy), managing roles/permissions is itself security-sensitive:
/// letting any authenticated user hit these endpoints directly would let them self-escalate regardless of
/// what the frontend hides.</summary>
[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RoleDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<RoleDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(RoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<RoleDto>> Update(long id, RoleRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>The permission-matrix screen's data source - every menu, with this role's current flags.</summary>
    [HttpGet("{roleId:long}/permissions")]
    public async Task<ActionResult<IReadOnlyList<MenuPermissionDto>>> GetPermissions(
        long roleId, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetPermissionsAsync(roleId, cancellationToken));
    }

    /// <summary>Replaces the whole matrix in one call - see IRoleService.SetPermissionsAsync.</summary>
    [HttpPut("{roleId:long}/permissions")]
    public async Task<IActionResult> SetPermissions(
        long roleId, SetPermissionsRequestDto request, CancellationToken cancellationToken)
    {
        await _roleService.SetPermissionsAsync(roleId, request, cancellationToken);
        return NoContent();
    }
}
