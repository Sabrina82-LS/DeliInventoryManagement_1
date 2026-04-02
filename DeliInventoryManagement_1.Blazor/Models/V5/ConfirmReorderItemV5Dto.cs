namespace DeliInventoryManagement_1.Blazor.Models.V5;

public sealed class ConfirmReorderItemV5Dto
{
    public string ProductId { get; set; } = string.Empty;
    public int OrderQty { get; set; }
}