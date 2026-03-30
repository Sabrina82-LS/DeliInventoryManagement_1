using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services.IService;

public interface IReorderService
{
    Task<List<ReorderListItemV5Dto>> GetReorderListAsync();
    Task<List<ReorderListItemV5Dto>> GetLowStockTop10Async();
    Task<bool> ConfirmReorderAsync(ConfirmReorderRequestV5Dto request);

}