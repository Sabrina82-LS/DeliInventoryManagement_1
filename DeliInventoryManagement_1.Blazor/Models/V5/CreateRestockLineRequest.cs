namespace DeliInventoryManagement_1.Blazor.Models.V5;

public class CreateRestockLineRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
}
