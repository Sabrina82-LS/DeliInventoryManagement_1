using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Services.IService;

public interface ISalesService
{
    Task<SaleV5?> GetSaleByIdAsync(string id);
    Task<SaleV5> CreateSaleAsync(SaleV5 sale);
    Task<IEnumerable<SaleV5>> GetAllSalesAsync();
    Task<IEnumerable<SaleV5>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
}