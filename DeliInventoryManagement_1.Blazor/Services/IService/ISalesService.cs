using DeliInventoryManagement_1.Blazor.Models.CreateRequest;
using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services.IService;

public interface ISalesService
{
    Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req);
}
