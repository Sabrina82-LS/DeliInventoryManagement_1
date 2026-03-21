using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Services
{
    public interface ISalesService
    {
        Task<Sales?> GetSaleByIdAsync(string id);
        Task<Sales> CreateSaleAsync(Sales sale);
        Task<IEnumerable<Sales>> GetAllSalesAsync();
        Task<IEnumerable<Sales>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}