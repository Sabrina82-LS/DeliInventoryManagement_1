using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services.IService;

public interface IPurchaseOrderService
{
    Task<List<ReorderOrderV5Dto>> GetOrdersAsync();
    Task<ReorderOrderV5Dto?> GetOrderByIdAsync(string id);
    Task<bool> MarkAsCompletedAsync(string id);
}