using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface IDashboardService
{
    // V5 direto
    Task<List<ProductV5Dto>> GetV5ProductsAsync();
    Task<List<SaleV5Dto>> GetV5SalesAsync();

    // Compatibilidade (páginas atuais)
    Task<List<ProductDto>> GetAllProductsAsync(string? search = null, string? categoryId = null);
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<List<SupplierDto>> GetAllSuppliersAsync();

    // Dashboard
    Task<ProductSummaryDto> GetProductSummaryAsync();
    Task<List<ProductDto>> GetLowStockProductsAsync(int top = 5);
    Task<int> GetSupplierCountAsync();
}
