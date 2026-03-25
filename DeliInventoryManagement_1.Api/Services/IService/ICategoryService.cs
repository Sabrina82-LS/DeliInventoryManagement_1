using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Services.IService;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(string id);
    Task<Category> CreateAsync(Category category);
    Task<Category?> UpdateAsync(string id, Category category);
    Task<bool> DeleteAsync(string id);
}
