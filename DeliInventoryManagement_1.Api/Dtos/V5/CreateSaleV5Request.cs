namespace DeliInventoryManagement_1.Api.Dtos.V5;

public class CreateSaleV5Request
{
    public DateTime? Date { get; set; }
    public List<CreateSaleLineV5Request> Lines { get; set; } = new();
}

public class CreateSaleLineV5Request
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
