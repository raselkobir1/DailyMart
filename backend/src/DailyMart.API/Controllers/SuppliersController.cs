using DailyMart.Application.Common.Models;
using DailyMart.Application.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SupplierDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(
        CreateSupplierRequestDto request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SupplierDto>> Update(
        long id, SupplierRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _supplierService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/ledger")]
    public async Task<ActionResult<PagedResult<SupplierLedgerEntryDto>>> GetLedger(
        long id, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetLedgerAsync(id, request, cancellationToken));
    }

    /// <summary>Module 11: records a payment made to a supplier as a new ledger entry.</summary>
    [HttpPost("{id:long}/payments")]
    public async Task<ActionResult<SupplierDto>> PaySupplier(
        long id, PaySupplierRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.PaySupplierAsync(id, request, cancellationToken));
    }

    /// <summary>Module 11: suppliers with an outstanding due, highest-due-first.</summary>
    [HttpGet("due-report")]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetDueReport(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetDueReportAsync(request, cancellationToken));
    }
}
