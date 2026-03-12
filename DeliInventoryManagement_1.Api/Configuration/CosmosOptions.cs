namespace DeliInventoryManagement_1.Api.Configuration;

public sealed class CosmosOptions
{
    public string AccountEndpoint { get; set; } = "";
    public string AccountKey { get; set; } = "";
    public string DatabaseId { get; set; } = "";

    public string PartitionKeyPath { get; set; } = "/pk";
    public string DefaultStorePk { get; set; } = "STORE#1";

    public CosmosContainers Containers { get; set; } = new();
    public Dictionary<string, string>? LegacyContainers { get; set; }
}

public sealed class CosmosContainers
{
    public string Products { get; set; } = "Products";
    public string Suppliers { get; set; } = "Suppliers";
    public string ReorderRules { get; set; } = "ReorderRules";
    public string Operations { get; set; } = "Operations";
    public string Users { get; set; } = "Users";
}
