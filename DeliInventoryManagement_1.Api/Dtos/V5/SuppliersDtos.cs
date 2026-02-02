namespace DeliInventoryManagement_1.Api.Dtos.V5;

public record CreateSupplierRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Notes
);

public record UpdateSupplierRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Notes
);
