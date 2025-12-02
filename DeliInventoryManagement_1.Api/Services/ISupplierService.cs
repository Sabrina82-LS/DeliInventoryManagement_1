using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(string id);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<Supplier?> UpdateAsync(string id, Supplier supplier);
    Task<bool> DeleteAsync(string id);
}
