using DeliInventoryManagement_1.Blazor.Models.Report;

namespace DeliInventoryManagement_1.Blazor.Services;

public interface IReportsService
{
    Task<List<SaleReportItem>> GetSalesAsync(DateTime? from, DateTime? to);
    Task<List<RestockReportItem>> GetRestocksAsync(DateTime? from, DateTime? to);
}
