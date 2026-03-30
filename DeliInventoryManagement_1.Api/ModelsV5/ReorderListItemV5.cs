namespace DeliInventoryManagement_1.Api.ModelsV5;

public class ReorderListItemV5
{
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string CategoryName { get; set; } = default!;

    public int CurrentQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public int SuggestedQty { get; set; }
    public int OrderQty { get; set; }

    public string SupplierId { get; set; } = default!;
    public string SupplierName { get; set; } = default!;
}