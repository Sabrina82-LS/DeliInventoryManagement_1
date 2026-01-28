namespace DeliInventoryManagement_1.Api.Dtos;

public class CreateSaleRequest
{
    public DateTime Date { get; set; }
    public List<CreateSaleLineRequest> Lines { get; set; } = new();
}
