using DailyMart.Application.Common.Models;
using FluentValidation;

namespace DailyMart.Application.Common.Validators;

/// <summary>
/// Shared validator for the pagination/filtering/sorting convention (CLAUDE.md §4/§9), reused by every
/// module's list endpoint - not just Module 0's audit log listing.
/// </summary>
public class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    private const int MaxPageSize = 100;

    public PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.SearchTerm)
            .MaximumLength(128);

        RuleFor(x => x.SortBy)
            .MaximumLength(64);
    }
}
