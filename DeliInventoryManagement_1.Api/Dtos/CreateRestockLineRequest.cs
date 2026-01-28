namespace DeliInventoryManagement_1.Api.Dtos;

public class CreateRestockLineRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
}
