using DailyMart.Application.Common.Models;
using DailyMart.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        CustomerRequestDto request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CustomerDto>> Update(
        long id, CustomerRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _customerService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/ledger")]
    public async Task<ActionResult<PagedResult<CustomerLedgerEntryDto>>> GetLedger(
        long id, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetLedgerAsync(id, request, cancellationToken));
    }

    /// <summary>Module 10: records a due payment as a new ledger entry, capped at the outstanding due.</summary>
    [HttpPost("{id:long}/payments")]
    public async Task<ActionResult<CustomerDto>> CollectPayment(
        long id, CollectCustomerPaymentRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _customerService.CollectPaymentAsync(id, request, cancellationToken));
    }

    /// <summary>Module 10: customers with an outstanding due, highest-due-first.</summary>
    [HttpGet("due-report")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetDueReport(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetDueReportAsync(request, cancellationToken));
    }
}
