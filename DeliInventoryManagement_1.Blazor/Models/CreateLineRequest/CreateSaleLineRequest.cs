namespace DeliInventoryManagement_1.Blazor.Models.CreateLineRequest;

public class CreateSaleLineRequest
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
