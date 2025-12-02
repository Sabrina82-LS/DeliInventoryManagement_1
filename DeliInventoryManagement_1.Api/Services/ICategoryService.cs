using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(string id);
    Task<Category> CreateAsync(Category category);
    Task<Category?> UpdateAsync(string id, Category category);
    Task<bool> DeleteAsync(string id);
}
