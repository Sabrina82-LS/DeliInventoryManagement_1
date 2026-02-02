using DeliInventoryManagement_1.Blazor.Models;
using DeliInventoryManagement_1.Blazor.Models.Legacy;
using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface IDashboardService
{
    // =========================
    // ✅ V5-first (padrão novo)
    // =========================
    Task<List<ProductV5Dto>> GetAllProductsV5Async(string? search = null, string? categoryId = null);
    Task<List<SaleV5Dto>> GetV5SalesAsync();

    // (Opcional) Se você quiser acessar a lista “crua” sem filtros:
    // Task<List<ProductV5Dto>> GetV5ProductsAsync();

    // =========================
    // 🗃️ Legacy (páginas antigas)
    // =========================
    Task<List<ProductDto>> GetAllProductsAsync(string? search = null, string? categoryId = null);
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<List<SupplierDto>> GetAllSuppliersAsync();

    // =========================
    // Dashboard summaries
    // =========================
    Task<ProductSummaryDto> GetProductSummaryAsync();
    Task<List<ProductDto>> GetLowStockProductsAsync(int top = 5);
    Task<int> GetSupplierCountAsync();
}
