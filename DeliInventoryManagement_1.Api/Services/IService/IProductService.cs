using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Services.IService;

public interface IProductService
{
    Task<PagedResult<ProductV5>> GetProductsAsync(ProductQueryParameters q);
    Task<ProductV5?> GetByIdAsync(string id);
    Task<ProductV5> CreateAsync(ProductV5 product);
    Task<ProductV5?> UpdateAsync(string id, ProductV5 product);
    Task<bool> DeleteAsync(string id);
    Task<ProductSummary> GetSummaryAsync();
}