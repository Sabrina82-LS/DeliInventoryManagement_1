namespace DeliInventoryManagement_1.Api.Dtos.V5.Events;

public sealed class RestockCreatedEventV5
{
    public string RestockId { get; set; } = "";
    public string SupplierId { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal TotalCost { get; set; }

    public List<RestockCreatedLineEventV5> Lines { get; set; } = new();
}

public sealed class RestockCreatedLineEventV5
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
}
