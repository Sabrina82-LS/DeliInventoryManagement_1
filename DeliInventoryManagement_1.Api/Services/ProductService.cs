using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services.IService;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Services;

public class ProductService : IProductService
{
    private readonly Container _container;
    private readonly IMemoryCache _cache;
    private readonly string _storePk;

    public ProductService(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        IMemoryCache cache)
    {
        _cache = cache;

        var opt = options.Value;
        _storePk = opt.DefaultStorePk;

        _container = cosmosClient.GetContainer(opt.DatabaseId, opt.Containers.Products);
    }

    private async Task<List<ProductV5>> LoadAllProductsAsync()
    {
        if (_cache.TryGetValue("products_all_v5", out List<ProductV5>? cached) && cached is not null)
            return cached;

        var query = new QueryDefinition("SELECT * FROM c WHERE c.pk = @pk")
            .WithParameter("@pk", _storePk);

        var iterator = _container.GetItemQueryIterator<ProductV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(_storePk)
            });

        var results = new List<ProductV5>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        _cache.Set("products_all_v5", results, TimeSpan.FromMinutes(1));
        return results;
    }

    public async Task<PagedResult<ProductV5>> GetProductsAsync(ProductQueryParameters q)
    {
        var items = await LoadAllProductsAsync();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLowerInvariant();

            items = items
                .Where(p => p.Name.ToLower().Contains(term) ||
                            p.CategoryName.ToLower().Contains(term))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(q.CategoryId))
        {
            items = items
                .Where(p => p.CategoryId == q.CategoryId)
                .ToList();
        }

        bool desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        items = (q.SortBy?.ToLowerInvariant()) switch
        {
            "price" => desc ? items.OrderByDescending(p => p.Price).ToList()
                            : items.OrderBy(p => p.Price).ToList(),

            "quantity" => desc ? items.OrderByDescending(p => p.Quantity).ToList()
                               : items.OrderBy(p => p.Quantity).ToList(),

            _ => desc ? items.OrderByDescending(p => p.Name).ToList()
                      : items.OrderBy(p => p.Name).ToList()
        };

        int total = items.Count;
        int skip = (q.Page - 1) * q.PageSize;
        var pageItems = items.Skip(skip).Take(q.PageSize).ToList();

        return new PagedResult<ProductV5>
        {
            Items = pageItems,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize
        };
    }

    public async Task<ProductV5?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<ProductV5>(
                id,
                new PartitionKey(_storePk));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ProductV5> CreateAsync(ProductV5 product)
    {
        if (string.IsNullOrWhiteSpace(product.Id))
            product.Id = Guid.NewGuid().ToString("n");

        product.Pk = _storePk;
        product.Type = "Product";
        product.CreatedAtUtc = DateTime.UtcNow;
        product.UpdatedAtUtc = DateTime.UtcNow;

        _cache.Remove("products_all_v5");

        var response = await _container.CreateItemAsync(product, new PartitionKey(product.Pk));
        return response.Resource;
    }

    public async Task<ProductV5?> UpdateAsync(string id, ProductV5 product)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
            return null;

        existing.Name = product.Name;
        existing.CategoryId = product.CategoryId;
        existing.CategoryName = product.CategoryName;
        existing.Quantity = product.Quantity;
        existing.Cost = product.Cost;
        existing.Price = product.Price;
        existing.ReorderLevel = product.ReorderLevel;
        existing.ReorderQty = product.ReorderQty;
        existing.IsActive = product.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        _cache.Remove("products_all_v5");

        var response = await _container.ReplaceItemAsync(existing, id, new PartitionKey(_storePk));
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<ProductV5>(id, new PartitionKey(_storePk));
            _cache.Remove("products_all_v5");
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