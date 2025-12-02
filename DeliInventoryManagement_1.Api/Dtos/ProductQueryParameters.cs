namespace DeliInventoryManagement_1.Api.Dtos;

public class ProductQueryParameters
{
    public string? Search { get; set; }      // name search
    public string? CategoryId { get; set; }  // filter by category
    public string? SortBy { get; set; } = "Name"; // Name, Price, Quantity
    public string? SortDir { get; set; } = "asc"; // asc/desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
