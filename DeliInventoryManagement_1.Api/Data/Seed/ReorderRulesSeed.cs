using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class ReorderRulesSeed
{
    public static List<ReorderRuleV5> GetRules(List<ProductV5> products, List<SupplierV5> suppliers)
    {
        var now = DateTime.UtcNow;
        var rules = new List<ReorderRuleV5>();

        if (products.Count == 0 || suppliers.Count == 0)
            return rules;

        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var supplier = suppliers[i % suppliers.Count];

            rules.Add(new ReorderRuleV5
            {
                Id = $"rr-{product.Id}",
                Pk = "STORE#1",
                Type = "ReorderRule",
                ProductId = product.Id,
                ProductName = product.Name,
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                ReorderLevel = product.ReorderLevel,
                ReorderQty = product.ReorderQty,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        return rules;
    }
}