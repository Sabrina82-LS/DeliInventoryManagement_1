namespace DeliInventoryManagement_1.Api.Dtos;

public class CreateSaleRequest
{
    public string ProductId { get; set; } = default!;
    public int Quantity { get; set; }
}
