using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Services;

public interface IProductService
{
    Task<PagedResult<Product>> GetProductsAsync(ProductQueryParameters query);
    Task<Product?> GetByIdAsync(string id);
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(string id, Product product);
    Task<bool> DeleteAsync(string id);
    Task<ProductSummary> GetSummaryAsync();
}
