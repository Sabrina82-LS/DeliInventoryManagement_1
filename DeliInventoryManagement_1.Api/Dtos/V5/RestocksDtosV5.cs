namespace DeliInventoryManagement_1.Api.Dtos.V5;

public sealed class CreateRestockRequestV5
{
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string SupplierId { get; set; } = "";
    public string SupplierName { get; set; } = "";

    public List<CreateRestockLineRequestV5> Lines { get; set; } = new();
}

public sealed class CreateRestockLineRequestV5
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
}
