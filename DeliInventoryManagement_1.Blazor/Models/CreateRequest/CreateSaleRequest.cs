using DeliInventoryManagement_1.Blazor.Models.CreateLineRequest;

namespace DeliInventoryManagement_1.Blazor.Models.CreateRequest;

public class CreateSaleRequest
{
    public DateTime Date { get; set; }
    public List<CreateSaleLineRequest> Lines { get; set; } = new();
}
