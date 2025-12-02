using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;

namespace DeliInventoryManagement_1.Api.Services;

public class ProductService : IProductService
{
    private readonly Container _container;
    private readonly IMemoryCache _cache;
    private const string TypeValue = "Product";

    public ProductService(CosmosClient cosmosClient, IConfiguration config, IMemoryCache cache)
    {
        _cache = cache;

        var cosmosSection = config.GetSection("CosmosDb");
        var databaseId = cosmosSection["DatabaseId"]!;
        var containerId = cosmosSection["ContainerId"]!;
        _container = cosmosClient.GetContainer(databaseId, containerId);
    }

    private async Task<List<Product>> LoadAllProductsAsync()
    {
        // Simple cache of all products (1 minute)
        if (_cache.TryGetValue("products_all", out List<Product>? cached) && cached is not null)
            return cached;

        var query = new QueryDefinition("SELECT * FROM c WHERE c.Type = @type")
            .WithParameter("@type", TypeValue);

        var iterator = _container.GetItemQueryIterator<Product>(query);
        var results = new List<Product>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        _cache.Set("products_all", results, TimeSpan.FromMinutes(1));
        return results;
    }

    public async Task<PagedResult<Product>> GetProductsAsync(ProductQueryParameters q)
    {
        var items = await LoadAllProductsAsync();

        // Searching
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLowerInvariant();
            items = items
                .Where(p => p.Name.ToLower().Contains(term) ||
                            p.CategoryName.ToLower().Contains(term))
                .ToList();
        }

        // Filter by category
        if (!string.IsNullOrWhiteSpace(q.CategoryId))
        {
            items = items
                .Where(p => p.CategoryId == q.CategoryId)
                .ToList();
        }

        // Sorting
        bool desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        items = (q.SortBy?.ToLower()) switch
        {
            "price" => desc ? items.OrderByDescending(p => p.Price).ToList()
                            : items.OrderBy(p => p.Price).ToList(),
            "quantity" => desc ? items.OrderByDescending(p => p.Quantity).ToList()
                               : items.OrderBy(p => p.Quantity).ToList(),
            _ => desc ? items.OrderByDescending(p => p.Name).ToList()
                      : items.OrderBy(p => p.Name).ToList()
        };

        // Paging
        int total = items.Count;
        int skip = (q.Page - 1) * q.PageSize;
        var pageItems = items.Skip(skip).Take(q.PageSize).ToList();

        return new PagedResult<Product>
        {
            Items = pageItems,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize
        };
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Product>(
                id,
                new PartitionKey(TypeValue));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.Type = TypeValue;

        if (string.IsNullOrEmpty(product.Id))
        {
            product.Id = Guid.NewGuid().ToString();
        }

        _cache.Remove("products_all");

        var response = await _container.CreateItemAsync(product, new PartitionKey(TypeValue));
        return response.Resource;
    }

    public async Task<Product?> UpdateAsync(string id, Product product)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null) return null;

        existing.Name = product.Name;
        existing.CategoryId = product.CategoryId;
        existing.CategoryName = product.CategoryName;
        existing.Quantity = product.Quantity;
        existing.Cost = product.Cost;
        existing.Price = product.Price;

        _cache.Remove("products_all");

        var response = await _container.ReplaceItemAsync(existing, id, new PartitionKey(TypeValue));
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<Product>(id, new PartitionKey(TypeValue));
            _cache.Remove("products_all");
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<ProductSummary> GetSummaryAsync()
    {
        var items = await LoadAllProductsAsync();

        int count = items.Count;
        int totalQty = items.Sum(p => p.Quantity);
        decimal totalCost = items.Sum(p => p.Cost * p.Quantity);
        decimal totalPrice = items.Sum(p => p.Price * p.Quantity);

        return new ProductSummary
        {
            Count = count,
            TotalQuantity = totalQty,
            TotalCostValue = totalCost,
            TotalPriceValue = totalPrice,
            AveragePrice = count == 0 ? 0 : items.Average(p => p.Price)
        };
    }
}
