using DailyMart.Application.Common.Models;
using DailyMart.Application.Purchases;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly IPurchaseReturnService _purchaseReturnService;

    public PurchasesController(IPurchaseService purchaseService, IPurchaseReturnService purchaseReturnService)
    {
        _purchaseService = purchaseService;
        _purchaseReturnService = purchaseReturnService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PurchaseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseDto>> Create(
        PurchaseRequestDto request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, purchase);
    }

    /// <summary>Full reverse-and-reapply under the hood - see PurchaseService.UpdateAsync.</summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<PurchaseDto>> Update(
        long id, PurchaseRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _purchaseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{purchaseId:long}/returns")]
    public async Task<ActionResult<PagedResult<PurchaseReturnDto>>> GetReturns(
        long purchaseId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseReturnService.GetPagedAsync(purchaseId, request, cancellationToken));
    }

    [HttpGet("{purchaseId:long}/returns/{returnId:long}")]
    public async Task<ActionResult<PurchaseReturnDto>> GetReturnById(
        long purchaseId, long returnId, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseReturnService.GetByIdAsync(returnId, cancellationToken));
    }

    /// <summary>Create + read only - no update/delete endpoint exists, see IPurchaseReturnService.</summary>
    [HttpPost("{purchaseId:long}/returns")]
    public async Task<ActionResult<PurchaseReturnDto>> CreateReturn(
        long purchaseId, PurchaseReturnRequestDto request, CancellationToken cancellationToken)
    {
        var purchaseReturn = await _purchaseReturnService.CreateAsync(purchaseId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetReturnById), new { purchaseId, returnId = purchaseReturn.Id }, purchaseReturn);
    }
}
