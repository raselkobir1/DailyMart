using DailyMart.Application.Common.Models;
using DailyMart.Application.Sales;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ISaleReturnService _saleReturnService;

    public SalesController(ISaleService saleService, ISaleReturnService saleReturnService)
    {
        _saleService = saleService;
        _saleReturnService = saleReturnService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SaleDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _saleService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SaleDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _saleService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Create-only - no update/delete endpoint exists, see ISaleService.</summary>
    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create(SaleRequestDto request, CancellationToken cancellationToken)
    {
        var sale = await _saleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    [HttpGet("{saleId:long}/returns")]
    public async Task<ActionResult<PagedResult<SaleReturnDto>>> GetReturns(
        long saleId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _saleReturnService.GetPagedAsync(saleId, request, cancellationToken));
    }

    [HttpGet("{saleId:long}/returns/{returnId:long}")]
    public async Task<ActionResult<SaleReturnDto>> GetReturnById(
        long saleId, long returnId, CancellationToken cancellationToken)
    {
        return Ok(await _saleReturnService.GetByIdAsync(returnId, cancellationToken));
    }

    /// <summary>Create + read only - no update/delete endpoint exists, see ISaleReturnService.</summary>
    [HttpPost("{saleId:long}/returns")]
    public async Task<ActionResult<SaleReturnDto>> CreateReturn(
        long saleId, SaleReturnRequestDto request, CancellationToken cancellationToken)
    {
        var saleReturn = await _saleReturnService.CreateAsync(saleId, request, cancellationToken);
        return CreatedAtAction(nameof(GetReturnById), new { saleId, returnId = saleReturn.Id }, saleReturn);
    }
}
