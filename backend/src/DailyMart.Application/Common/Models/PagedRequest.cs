namespace DailyMart.Application.Common.Models;

/// <summary>
/// Shared pagination/filtering/sorting request, reused by every list endpoint (CLAUDE.md §4/§9).
/// </summary>
public class PagedRequest
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? SearchTerm { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
