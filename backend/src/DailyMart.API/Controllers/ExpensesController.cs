using DailyMart.Application.Common.Models;
using DailyMart.Application.Expenses;
using DailyMart.Domain.Expenses;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> GetPaged(
        [FromQuery] PagedRequest request,
        [FromQuery] ExpenseCategory? category,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        return Ok(await _expenseService.GetPagedAsync(request, category, fromDate, toDate, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ExpenseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _expenseService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(ExpenseRequestDto request, CancellationToken cancellationToken)
    {
        var expense = await _expenseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ExpenseDto>> Update(
        long id, ExpenseRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _expenseService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _expenseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
