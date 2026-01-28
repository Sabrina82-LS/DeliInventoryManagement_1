using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

// Interface do Restock (Blazor -> API)
public interface IRestockService
{
    // POST /api/restocks
    Task CreateAsync(CreateRestockRequest request);
}
