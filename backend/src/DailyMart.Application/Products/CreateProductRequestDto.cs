namespace DailyMart.Application.Products;

/// <summary>Adds CurrentStock (the opening stock) - the one field only creation can set.</summary>
public class CreateProductRequestDto : ProductRequestDto
{
    public decimal CurrentStock { get; init; }
}
