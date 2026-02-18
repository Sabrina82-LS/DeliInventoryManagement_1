using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface IDashboardService
{
    // V5 Products
    Task<List<ProductV5Dto>> GetAllProductsAsync(string? search = null, string? categoryId = null);
    Task<List<CategoryV5Dto>> GetAllCategoriesAsync();

    //Adicionar isto:
    Task<List<SaleV5Dto>> GetAllSalesAsync();

    // Dashboard summary
    Task<ProductSummaryDto> GetProductSummaryAsync();
    Task<List<ProductV5Dto>> GetLowStockProductsAsync(int top = 5);

    // Optional (if you use suppliers on dashboard)
    Task<int> GetSupplierCountAsync();
}
