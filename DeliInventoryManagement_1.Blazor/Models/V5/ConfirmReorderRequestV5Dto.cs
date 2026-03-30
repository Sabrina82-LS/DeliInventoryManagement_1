namespace DeliInventoryManagement_1.Blazor.Models.V5;

public sealed class ConfirmReorderRequestV5Dto
{
    public List<ConfirmReorderItemV5Dto> Items { get; set; } = new();
}