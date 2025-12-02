using DeliInventoryManagement_1.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;

namespace DeliInventoryManagement_1.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly Container _container;
    private readonly IMemoryCache _cache;
    private const string TypeValue = "Category";

    public CategoryService(CosmosClient cosmosClient, IConfiguration config, IMemoryCache cache)
    {
        _cache = cache;

        var cosmosSection = config.GetSection("CosmosDb");
        var databaseId = cosmosSection["DatabaseId"]!;
        var containerId = cosmosSection["ContainerId"]!;
        _container = cosmosClient.GetContainer(databaseId, containerId);
    }

    private async Task<List<Category>> LoadAllCategoriesAsync()
    {
        if (_cache.TryGetValue("categories_all", out List<Category>? cached) && cached is not null)
            return cached;

        var query = new QueryDefinition("SELECT * FROM c WHERE c.Type = @type")
            .WithParameter("@type", TypeValue);

        var iterator = _container.GetItemQueryIterator<Category>(query);
        var results = new List<Category>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        _cache.Set("categories_all", results, TimeSpan.FromMinutes(5));
        return results;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await LoadAllCategoriesAsync();
    }

    public async Task<Category?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Category>(
                id,
                new PartitionKey(TypeValue));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Category> CreateAsync(Category category)
    {
        category.Type = TypeValue;

        if (string.IsNullOrEmpty(category.Id))
        {
            category.Id = Guid.NewGuid().ToString();
        }

        _cache.Remove("categories_all");

        var response = await _container.CreateItemAsync(category, new PartitionKey(TypeValue));
        return response.Resource;
    }


    public async Task<Category?> UpdateAsync(string id, Category category)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null) return null;

        existing.Name = category.Name;
        existing.Description = category.Description;

        _cache.Remove("categories_all");

        var response = await _container.ReplaceItemAsync(existing, id, new PartitionKey(TypeValue));
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<Category>(id, new PartitionKey(TypeValue));
            _cache.Remove("categories_all");
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
