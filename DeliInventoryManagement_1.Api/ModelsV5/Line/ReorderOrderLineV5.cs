namespace DeliInventoryManagement_1.Api.ModelsV5;

public class ReorderOrderLineV5
{
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int QuantityRequested { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}