using DailyMart.Application.Common.Models;
using DailyMart.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<InventoryAdjustmentDto>> RecordAdjustment(
        StockAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.RecordAdjustmentAsync(request, cancellationToken));
    }

    [HttpPost("damaged")]
    public async Task<ActionResult<InventoryAdjustmentDto>> RecordDamaged(
        DamagedStockRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.RecordDamagedAsync(request, cancellationToken));
    }

    /// <summary>Optionally filtered to one product via ?productId=.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<InventoryTransactionDto>>> GetTransactions(
        [FromQuery] PagedRequest request, [FromQuery] long? productId, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.GetTransactionHistoryAsync(request, productId, cancellationToken));
    }
}
