using DailyMart.Domain.Common;

namespace DailyMart.Domain.Products;

/// <summary>
/// The item catalog. CurrentStock is set once at creation (opening stock) - once Purchase/Inventory/POS
/// Sales exist (Modules 7-9), they own every further stock movement via an auditable
/// InventoryTransaction row each; nothing here mutates CurrentStock after creation.
/// </summary>
public class Product : AuditableEntity
{
    public string Code { get; set; } = string.Empty;

    /// <summary>User-supplied, or a generated EAN-13 value if none was given - see ProductService.</summary>
    public string Barcode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public long CategoryId { get; set; }

    public long? BrandId { get; set; }

    public long UnitId { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal? WholesalePrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal TaxPercentage { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }

    /// <summary>Explicit override for the "selling price >= purchase price" business rule (e.g. clearance items).</summary>
    public bool AllowPriceBelowCost { get; set; }

    public string? ImageUrl { get; set; }
}
