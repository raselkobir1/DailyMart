using DailyMart.Application.Common.Models;
using DailyMart.Application.MasterData;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/units")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UnitDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create(UnitRequestDto request, CancellationToken cancellationToken)
    {
        var unit = await _unitService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<UnitDto>> Update(long id, UnitRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _unitService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
