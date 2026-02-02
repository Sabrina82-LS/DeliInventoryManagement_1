using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface ISuppliersServiceV5
{
    Task<List<SupplierV5Dto>> GetAllAsync();
    Task<SupplierV5Dto?> GetByIdAsync(string id);
    Task<SupplierV5Dto> CreateAsync(SupplierV5Dto dto); // simples (mínimo)
    Task<SupplierV5Dto> UpdateAsync(string id, SupplierV5Dto dto);
    Task DeleteAsync(string id);
}
