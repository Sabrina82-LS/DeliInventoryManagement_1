using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class RestockV5 : CosmosDocument
{
    public RestockV5()
    {
        Type = "Restock";
        Pk = "ops";
    }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string SupplierId { get; set; } = "";
    public string SupplierName { get; set; } = "";

    public List<RestockLineV5> Lines { get; set; } = new();

    public decimal TotalCost => Lines.Sum(l => l.LineTotal);

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RestockLineV5
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal LineTotal => Quantity * CostPerUnit;
}
