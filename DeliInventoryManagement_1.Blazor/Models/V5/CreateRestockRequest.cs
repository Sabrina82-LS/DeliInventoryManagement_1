namespace DeliInventoryManagement_1.Blazor.Models.V5;

public class CreateRestockRequest
{
    public DateTime Date { get; set; }

    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public List<CreateRestockLineRequest> Lines { get; set; } = new();

}
