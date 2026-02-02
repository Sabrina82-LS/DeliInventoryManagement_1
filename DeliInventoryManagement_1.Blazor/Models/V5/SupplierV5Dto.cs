namespace DeliInventoryManagement_1.Blazor.Models.V5;

public class SupplierV5Dto
{
    public string Id { get; set; } = "";
    public string Pk { get; set; } = "supplier";
    public string Type { get; set; } = "Supplier";

    public string Name { get; set; } = "";

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
