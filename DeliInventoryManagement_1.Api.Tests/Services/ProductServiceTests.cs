using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;

var cosmosOptions = Options.Create(new CosmosOptions
{
    DatabaseId = "TestDb",
    DefaultStorePk = "STORE#1",
    Containers = new CosmosContainers
    {
        Products = "Products",
        Suppliers = "Suppliers",
        ReorderRules = "ReorderRules",
        Operations = "Operations",
        Users = "Users"
    }
});