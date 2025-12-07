using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface IDashboardService
{
    Task<ProductSummaryDto> GetProductSummaryAsync();
    Task<int> GetSupplierCountAsync();
    Task<List<ProductDto>> GetLowStockProductsAsync(int top);
}
