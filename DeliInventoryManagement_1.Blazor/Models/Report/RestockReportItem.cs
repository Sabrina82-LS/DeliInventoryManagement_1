namespace DeliInventoryManagement_1.Blazor.Models.Report;

public class RestockReportItem
{
    public string Id { get; set; } = "";
    public DateTime Date { get; set; }

    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";

    public int QuantityAdded { get; set; }
    public decimal Cost { get; set; }

    public string SupplierName { get; set; } = "";
}
