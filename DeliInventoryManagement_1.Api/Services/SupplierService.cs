using DeliInventoryManagement_1.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;

namespace DeliInventoryManagement_1.Api.Services;

public class SupplierService : ISupplierService
{
    private readonly Container _container;
    private readonly IMemoryCache _cache;
    private const string TypeValue = "Supplier";

    public SupplierService(CosmosClient cosmosClient, IConfiguration config, IMemoryCache cache)
    {
        _cache = cache;

        var cosmosSection = config.GetSection("CosmosDb");
        var databaseId = cosmosSection["DatabaseId"]!;
        var containerId = cosmosSection["ContainerId"]!;
        _container = cosmosClient.GetContainer(databaseId, containerId);
    }

    private async Task<List<Supplier>> LoadAllSuppliersAsync()
    {
        if (_cache.TryGetValue("suppliers_all", out List<Supplier>? cached) && cached is not null)
            return cached;

        var query = new QueryDefinition("SELECT * FROM c WHERE c.Type = @type")
            .WithParameter("@type", TypeValue);

        var iterator = _container.GetItemQueryIterator<Supplier>(query);
        var results = new List<Supplier>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        _cache.Set("suppliers_all", results, TimeSpan.FromMinutes(5));
        return results;
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync()
    {
        return await LoadAllSuppliersAsync();
    }

    public async Task<Supplier?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Supplier>(
                id,
                new PartitionKey(TypeValue));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        supplier.Type = TypeValue;

        if (string.IsNullOrEmpty(supplier.Id))
        {
            supplier.Id = Guid.NewGuid().ToString();
        }

        _cache.Remove("suppliers_all");

        var response = await _container.CreateItemAsync(supplier, new PartitionKey(TypeValue));
        return response.Resource;
    }


    public async Task<Supplier?> UpdateAsync(string id, Supplier supplier)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null) return null;

        existing.Name = supplier.Name;
        existing.ContactName = supplier.ContactName;
        existing.Phone = supplier.Phone;
        existing.Email = supplier.Email;
        existing.Address = supplier.Address;
        existing.Notes = supplier.Notes;

        _cache.Remove("suppliers_all");

        var response = await _container.ReplaceItemAsync(existing, id, new PartitionKey(TypeValue));
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<Supplier>(id, new PartitionKey(TypeValue));
            _cache.Remove("suppliers_all");
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
