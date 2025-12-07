using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface IDashboardService
{
    Task<ProductSummaryDto> GetProductSummaryAsync();
    Task<int> GetSupplierCountAsync();
    Task<List<ProductDto>> GetLowStockProductsAsync(int top);

    Task<List<ProductDto>> GetAllProductsAsync(string? search, string? category);
    Task<List<CategoryDto>> GetAllCategoriesAsync();   // <-- this signature
    Task<List<SupplierDto>> GetAllSuppliersAsync();    // <-- and this
}
