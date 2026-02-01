using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Data;

public class CosmosContainerFactory
{
    public const string StorePk = "STORE#1";

    private readonly CosmosClient _cosmos;
    private readonly IConfiguration _cfg;

    public CosmosContainerFactory(CosmosClient cosmos, IConfiguration cfg)
    {
        _cosmos = cosmos;
        _cfg = cfg;
    }

    private string DbId =>
        _cfg.GetSection("CosmosDb")["DatabaseId"]
        ?? _cfg.GetSection("CosmosDb")["DatabaseName"]
        ?? "DeliInventoryDb";

    public Container Products()
    {
        var id = _cfg.GetSection("CosmosDb")["ProductsContainerId"] ?? "Products";
        return _cosmos.GetContainer(DbId, id);
    }

    public Container Operations()
    {
        var id = _cfg.GetSection("CosmosDb")["OperationsContainerId"] ?? "Operations";
        return _cosmos.GetContainer(DbId, id);
    }

    public Container Suppliers()
    {
        var id = _cfg.GetSection("CosmosDb")["SuppliersContainerId"] ?? "Suppliers";
        return _cosmos.GetContainer(DbId, id);
    }

    public Container ReorderRules()
    {
        var id = _cfg.GetSection("CosmosDb")["ReorderRulesContainerId"] ?? "ReorderRules";
        return _cosmos.GetContainer(DbId, id);
    }
}
