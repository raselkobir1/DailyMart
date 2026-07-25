namespace DailyMart.Application.Products;

public class ProductImportResultDto
{
    public int TotalRows { get; init; }

    public int Created { get; init; }

    public int Updated { get; init; }

    public IReadOnlyList<ProductImportRowErrorDto> Errors { get; init; } = [];
}

public class ProductImportRowErrorDto
{
    /// <summary>The spreadsheet's own row number (1-based, header is row 1) - what the user sees when they
    /// open the file, not a 0-based data index.</summary>
    public int RowNumber { get; init; }

    public string Message { get; init; } = string.Empty;
}
