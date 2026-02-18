namespace DeliInventoryManagement_1.Blazor.Models.V5;

public class ProductSummaryDto
{
    public int Count { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalPriceValue { get; set; }
    public decimal AveragePrice { get; set; }
}
