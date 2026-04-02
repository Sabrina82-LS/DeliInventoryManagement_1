namespace DeliInventoryManagement_1.Api.ModelsV5;

public class ConfirmReorderRequestV5
{
    public List<ConfirmReorderItemV5> Items { get; set; } = new();
}

public class ConfirmReorderItemV5
{
    public string ProductId { get; set; } = default!;
    public int OrderQty { get; set; }
}