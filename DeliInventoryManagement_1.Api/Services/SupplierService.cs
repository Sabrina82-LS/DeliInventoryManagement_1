using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services.IService;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Services;

public class SupplierService : ISupplierService
{
    private readonly Container _container;
    private readonly IMemoryCache _cache;
    private readonly string _storePk;

    public SupplierService(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        IMemoryCache cache)
    {
        _cache = cache;

        var opt = options.Value;
        _storePk = opt.DefaultStorePk;

        _container = cosmosClient.GetContainer(opt.DatabaseId, opt.Containers.Suppliers);
    }

    private async Task<List<SupplierV5>> LoadAllSuppliersAsync()
    {
        if (_cache.TryGetValue("suppliers_all_v5", out List<SupplierV5>? cached) && cached is not null)
            return cached;

        var query = new QueryDefinition("SELECT * FROM c WHERE c.pk = @pk")
            .WithParameter("@pk", _storePk);

        var iterator = _container.GetItemQueryIterator<SupplierV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(_storePk)
            });

        var results = new List<SupplierV5>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        _cache.Set("suppliers_all_v5", results, TimeSpan.FromMinutes(5));
        return results;
    }

    public async Task<IReadOnlyList<SupplierV5>> GetAllAsync()
    {
        return await LoadAllSuppliersAsync();
    }

    public async Task<SupplierV5?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<SupplierV5>(
                id,
                new PartitionKey(_storePk));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<SupplierV5> CreateAsync(SupplierV5 supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Id))
            supplier.Id = Guid.NewGuid().ToString("n");

        supplier.Pk = _storePk;
        supplier.Type = "Supplier";
        supplier.CreatedAtUtc = DateTime.UtcNow;
        supplier.UpdatedAtUtc = DateTime.UtcNow;

        _cache.Remove("suppliers_all_v5");

        var response = await _container.CreateItemAsync(supplier, new PartitionKey(supplier.Pk));
        return response.Resource;
    }

    public async Task<SupplierV5?> UpdateAsync(string id, SupplierV5 supplier)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
            return null;

        existing.Name = supplier.Name;
        existing.Email = supplier.Email;
        existing.Phone = supplier.Phone;
        existing.Notes = supplier.Notes;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        _cache.Remove("suppliers_all_v5");

        var response = await _container.ReplaceItemAsync(existing, id, new PartitionKey(_storePk));
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<SupplierV5>(id, new PartitionKey(_storePk));
            _cache.Remove("suppliers_all_v5");
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}