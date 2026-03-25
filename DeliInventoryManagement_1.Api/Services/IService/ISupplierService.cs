using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Services.IService;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierV5>> GetAllAsync();
    Task<SupplierV5?> GetByIdAsync(string id);
    Task<SupplierV5> CreateAsync(SupplierV5 supplier);
    Task<SupplierV5?> UpdateAsync(string id, SupplierV5 supplier);
    Task<bool> DeleteAsync(string id);
}