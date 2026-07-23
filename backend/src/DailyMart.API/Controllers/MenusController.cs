using DailyMart.Application.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Admin-only - see RolesController's doc comment for why.</summary>
[ApiController]
[Route("api/menus")]
[Authorize(Roles = "Admin")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MenuDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<MenuDto>> Create(CreateMenuRequestDto request, CancellationToken cancellationToken)
    {
        var menu = await _menuService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = menu.Id }, menu);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<MenuDto>> Update(long id, MenuRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _menuService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _menuService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
