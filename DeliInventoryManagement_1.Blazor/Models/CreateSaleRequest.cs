namespace DeliInventoryManagement_1.Blazor.Models;

public class CreateSaleRequest
{
    public DateTime Date { get; set; }
    public List<CreateSaleLineRequest> Lines { get; set; } = new();
}
