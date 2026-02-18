namespace DeliInventoryManagement_1.Blazor.Models.Report;

public class SaleReportItem
{
    public string Id { get; set; } = "";
    public DateTime Date { get; set; }

    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
