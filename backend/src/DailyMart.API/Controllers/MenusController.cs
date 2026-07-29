using DailyMart.Application.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Unlike Users/Roles (genuinely per-tenant, see RolesController's doc comment), Menu is one
/// shared/global table every tenant reads (CLAUDE.md §4) - a tenant's own "Admin" role has no authority
/// over it, only read access to support screens like Permissions that need the menu list. Mutating it is
/// "PlatformAdminOnly" (same policy as api/platform/* - see PlatformTenantsController's doc comment):
/// letting any tenant's own Admin rename/delete a menu would let them vandalize navigation for every
/// other tenant on the platform, not just their own.</summary>
[ApiController]
[Route("api/menus")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<MenuDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MenuDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<ActionResult<MenuDto>> Create(CreateMenuRequestDto request, CancellationToken cancellationToken)
    {
        var menu = await _menuService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = menu.Id }, menu);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<ActionResult<MenuDto>> Update(long id, MenuRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _menuService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
