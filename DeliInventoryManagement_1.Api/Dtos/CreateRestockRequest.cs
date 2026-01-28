namespace DeliInventoryManagement_1.Api.Dtos;

public class CreateRestockRequest
{
    public DateTime Date { get; set; }

    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public List<CreateRestockLineRequest> Lines { get; set; } = new();
}
