using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services.IService;

public interface IDashboardService
{
    Task<List<ProductV5Dto>> GetAllProductsAsync(string? search = null, string? categoryId = null);
    Task<List<CategoryV5Dto>> GetAllCategoriesAsync();
    Task<List<SaleV5Dto>> GetAllSalesAsync();
    Task<List<ReorderRuleV5Dto>> GetAllReorderRulesAsync();

    Task<ProductSummaryDto> GetProductSummaryAsync();
    Task<List<ReorderListItemV5Dto>> GetLowStockTop10Async();
    Task<List<ReorderListItemV5Dto>> GetPendingLowStockTop10Async();
    Task<int> GetSupplierCountAsync();
}