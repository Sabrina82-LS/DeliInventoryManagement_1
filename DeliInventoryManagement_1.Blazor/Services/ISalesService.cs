using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface ISalesService
{
    Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req);
}
