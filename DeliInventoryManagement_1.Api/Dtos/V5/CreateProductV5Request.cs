namespace DeliInventoryManagement_1.Api.Dtos.V5;

public class CreateProductV5Request
{
    public string Name { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }

    public int ReorderLevel { get; set; } = 5;
    public int ReorderQty { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}
