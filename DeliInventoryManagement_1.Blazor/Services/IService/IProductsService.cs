using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services.IService;

public interface IProductsService
{
    Task<List<ProductV5Dto>> GetProductsAsync();
    Task<ProductV5Dto?> GetProductByIdAsync(string id);
    Task<bool> CreateProductAsync(ProductV5Dto product);
    Task<bool> UpdateProductAsync(string id, ProductV5Dto product);
    Task<bool> DeleteProductAsync(string id);
}