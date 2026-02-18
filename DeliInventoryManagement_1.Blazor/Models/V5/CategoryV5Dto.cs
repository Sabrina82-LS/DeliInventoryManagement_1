namespace DeliInventoryManagement_1.Blazor.Models.V5;

public class CategoryV5Dto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Category";

}
