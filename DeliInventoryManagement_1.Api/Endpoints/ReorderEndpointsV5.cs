using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

public static class ReorderEndpointsV5
{
    public static IEndpointRouteBuilder MapV5Reorder(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v5/reorder")
            .WithTags("Reorder V5")
            .AllowAnonymous();
        // Later, when auth is fully aligned again:
        // .RequireAuthorization("AdminOrStaff");

        group.MapGet("/low-stock-top10", GetLowStockTop10);
        group.MapGet("/pending-low-stock-top10", GetPendingLowStockTop10);
        group.MapGet("/list", GetReorderList);
        group.MapPost("/confirm", ConfirmReorder);

        group.MapGet("/orders", GetPurchaseOrders);
        group.MapGet("/orders/{id}", GetPurchaseOrderById);
        group.MapPut("/orders/{id}/complete", CompletePurchaseOrder);

        return app;
    }

    private static async Task<IResult> GetLowStockTop10(
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            var dbId = config["CosmosDb:DatabaseId"];
            var productsContainerId = config["CosmosDb:Containers:Products"];
            var rulesContainerId = config["CosmosDb:Containers:ReorderRules"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(productsContainerId))
                return Results.Problem("CosmosDb:Containers:Products is not configured.");

            if (string.IsNullOrWhiteSpace(rulesContainerId))
                return Results.Problem("CosmosDb:Containers:ReorderRules is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var productsContainer = db.GetContainer(productsContainerId);
            var rulesContainer = db.GetContainer(rulesContainerId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var products = await ReadAllProducts(productsContainer, pk);
            var rules = await ReadAllRules(rulesContainer, pk);
            var pendingOrders = await ReadPendingOrders(operationsContainer, pk);

            var orderedProductIds = pendingOrders
                .SelectMany(o => o.Lines ?? new List<ReorderOrderLineV5>())
                .Select(l => l.ProductId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lowStockItems = (
                from p in products
                join r in rules on p.Id equals r.ProductId
                where p.Quantity <= r.ReorderLevel
                   && !orderedProductIds.Contains(p.Id)
                orderby p.Quantity ascending, p.Name
                select new ReorderListItemV5
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.CategoryName,
                    CurrentQuantity = p.Quantity,
                    ReorderLevel = r.ReorderLevel,
                    SuggestedQty = r.ReorderQty,
                    OrderQty = r.ReorderQty,
                    SupplierId = r.SupplierId,
                    SupplierName = r.SupplierName
                })
                .Take(10)
                .ToList();

            return Results.Ok(lowStockItems);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error loading low stock top 10",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetPendingLowStockTop10(
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            var dbId = config["CosmosDb:DatabaseId"];
            var productsContainerId = config["CosmosDb:Containers:Products"];
            var rulesContainerId = config["CosmosDb:Containers:ReorderRules"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(productsContainerId))
                return Results.Problem("CosmosDb:Containers:Products is not configured.");

            if (string.IsNullOrWhiteSpace(rulesContainerId))
                return Results.Problem("CosmosDb:Containers:ReorderRules is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var productsContainer = db.GetContainer(productsContainerId);
            var rulesContainer = db.GetContainer(rulesContainerId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var products = await ReadAllProducts(productsContainer, pk);
            var rules = await ReadAllRules(rulesContainer, pk);
            var pendingOrders = await ReadPendingOrders(operationsContainer, pk);

            var orderedProductIds = pendingOrders
                .SelectMany(o => o.Lines ?? new List<ReorderOrderLineV5>())
                .Select(l => l.ProductId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var pendingLowStockItems = (
                from p in products
                join r in rules on p.Id equals r.ProductId
                where p.Quantity <= r.ReorderLevel
                   && orderedProductIds.Contains(p.Id)
                orderby p.Quantity ascending, p.Name
                select new ReorderListItemV5
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.CategoryName,
                    CurrentQuantity = p.Quantity,
                    ReorderLevel = r.ReorderLevel,
                    SuggestedQty = r.ReorderQty,
                    OrderQty = r.ReorderQty,
                    SupplierId = r.SupplierId,
                    SupplierName = r.SupplierName
                })
                .Take(10)
                .ToList();

            return Results.Ok(pendingLowStockItems);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error loading pending low stock top 10",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetReorderList(
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            var dbId = config["CosmosDb:DatabaseId"];
            var productsContainerId = config["CosmosDb:Containers:Products"];
            var rulesContainerId = config["CosmosDb:Containers:ReorderRules"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(productsContainerId))
                return Results.Problem("CosmosDb:Containers:Products is not configured.");

            if (string.IsNullOrWhiteSpace(rulesContainerId))
                return Results.Problem("CosmosDb:Containers:ReorderRules is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var productsContainer = db.GetContainer(productsContainerId);
            var rulesContainer = db.GetContainer(rulesContainerId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var products = await ReadAllProducts(productsContainer, pk);
            var rules = await ReadAllRules(rulesContainer, pk);
            var pendingOrders = await ReadPendingOrders(operationsContainer, pk);

            var orderedProductIds = pendingOrders
                .SelectMany(o => o.Lines ?? new List<ReorderOrderLineV5>())
                .Select(l => l.ProductId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var reorderList = (
                from p in products
                join r in rules on p.Id equals r.ProductId
                where p.Quantity <= r.ReorderLevel
                   && !orderedProductIds.Contains(p.Id)
                orderby r.SupplierName, p.Name
                select new ReorderListItemV5
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.CategoryName,
                    CurrentQuantity = p.Quantity,
                    ReorderLevel = r.ReorderLevel,
                    SuggestedQty = r.ReorderQty,
                    OrderQty = r.ReorderQty,
                    SupplierId = r.SupplierId,
                    SupplierName = r.SupplierName
                })
                .ToList();

            return Results.Ok(reorderList);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error loading reorder list",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ConfirmReorder(
        ConfirmReorderRequestV5 request,
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            if (request is null || request.Items is null || !request.Items.Any())
            {
                return Results.BadRequest("No reorder items were provided.");
            }

            var dbId = config["CosmosDb:DatabaseId"];
            var productsContainerId = config["CosmosDb:Containers:Products"];
            var rulesContainerId = config["CosmosDb:Containers:ReorderRules"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(productsContainerId))
                return Results.Problem("CosmosDb:Containers:Products is not configured.");

            if (string.IsNullOrWhiteSpace(rulesContainerId))
                return Results.Problem("CosmosDb:Containers:ReorderRules is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var productsContainer = db.GetContainer(productsContainerId);
            var rulesContainer = db.GetContainer(rulesContainerId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var products = await ReadAllProducts(productsContainer, pk);
            var rules = await ReadAllRules(rulesContainer, pk);

            var selectedItems = (
                from req in request.Items
                join p in products on req.ProductId equals p.Id
                join r in rules on p.Id equals r.ProductId
                where req.OrderQty > 0
                select new
                {
                    Product = p,
                    Rule = r,
                    OrderQty = req.OrderQty
                })
                .ToList();

            if (!selectedItems.Any())
            {
                return Results.BadRequest("No valid reorder items with quantity greater than zero were found.");
            }

            var pendingOrders = await ReadPendingOrders(operationsContainer, pk);
            var pendingProductIds = pendingOrders
                .SelectMany(o => o.Lines ?? new List<ReorderOrderLineV5>())
                .Select(l => l.ProductId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            selectedItems = selectedItems
                .Where(x => !pendingProductIds.Contains(x.Product.Id))
                .ToList();

            if (!selectedItems.Any())
            {
                return Results.BadRequest("All selected products already have pending purchase orders.");
            }

            var groupedBySupplier = selectedItems
                .GroupBy(x => new { x.Rule.SupplierId, x.Rule.SupplierName });

            var createdOrders = new List<ReorderOrderV5>();

            foreach (var supplierGroup in groupedBySupplier)
            {
                var now = DateTime.UtcNow;

                var order = new ReorderOrderV5
                {
                    Id = $"ro-{Guid.NewGuid():N}",
                    Pk = pk,
                    Type = "ReorderOrder",
                    SupplierId = supplierGroup.Key.SupplierId,
                    SupplierName = supplierGroup.Key.SupplierName,
                    Status = "Pending",
                    Date = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Lines = new List<ReorderOrderLineV5>()
                };

                foreach (var item in supplierGroup)
                {
                    var line = new ReorderOrderLineV5
                    {
                        ProductId = item.Product.Id,
                        ProductName = item.Product.Name,
                        QuantityRequested = item.OrderQty,
                        UnitCost = item.Product.Cost,
                        LineTotal = item.Product.Cost * item.OrderQty
                    };

                    order.Lines.Add(line);
                }

                order.Total = order.Lines.Sum(x => x.LineTotal);

                await operationsContainer.CreateItemAsync(order, new PartitionKey(pk));
                createdOrders.Add(order);
            }

            return Results.Ok(createdOrders);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error confirming reorder",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<List<ProductV5>> ReadAllProducts(Container container, string pk)
    {
        var items = new List<ProductV5>();

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'Product'")
            .WithParameter("@pk", pk);

        using var iterator = container.GetItemQueryIterator<ProductV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(pk)
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items;
    }

    private static async Task<List<ReorderRuleV5>> ReadAllRules(Container container, string pk)
    {
        var items = new List<ReorderRuleV5>();

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'ReorderRule' AND c.isActive = true")
            .WithParameter("@pk", pk);

        using var iterator = container.GetItemQueryIterator<ReorderRuleV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(pk)
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items;
    }

    private static async Task<List<ReorderOrderV5>> ReadPendingOrders(Container container, string pk)
    {
        var items = new List<ReorderOrderV5>();

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'ReorderOrder' AND c.status = 'Pending'")
            .WithParameter("@pk", pk);

        using var iterator = container.GetItemQueryIterator<ReorderOrderV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(pk)
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items;
    }

    private static async Task<IResult> GetPurchaseOrders(
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            var dbId = config["CosmosDb:DatabaseId"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var items = new List<ReorderOrderV5>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'ReorderOrder' ORDER BY c.date DESC")
                .WithParameter("@pk", pk);

            using var iterator = operationsContainer.GetItemQueryIterator<ReorderOrderV5>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(pk)
                });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return Results.Ok(items);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error loading purchase orders",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetPurchaseOrderById(
        string id,
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            var dbId = config["CosmosDb:DatabaseId"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var response = await operationsContainer.ReadItemAsync<ReorderOrderV5>(
                id,
                new PartitionKey(pk));

            return Results.Ok(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Results.NotFound($"Purchase order '{id}' was not found.");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error loading purchase order",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> CompletePurchaseOrder(
        string id,
        CosmosClient cosmosClient,
        IConfiguration config)
    {
        try
        {
            var dbId = config["CosmosDb:DatabaseId"];
            var operationsContainerId = config["CosmosDb:Containers:Operations"];
            var pk = config["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(operationsContainerId))
                return Results.Problem("CosmosDb:Containers:Operations is not configured.");

            var db = cosmosClient.GetDatabase(dbId);
            var operationsContainer = db.GetContainer(operationsContainerId);

            var response = await operationsContainer.ReadItemAsync<ReorderOrderV5>(
                id,
                new PartitionKey(pk));

            var order = response.Resource;

            if (order is null)
                return Results.NotFound($"Purchase order '{id}' was not found.");

            if (string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(order);

            order.Status = "Completed";
            order.UpdatedAtUtc = DateTime.UtcNow;

            var updated = await operationsContainer.ReplaceItemAsync(
                order,
                order.Id,
                new PartitionKey(pk));

            return Results.Ok(updated.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Results.NotFound($"Purchase order '{id}' was not found.");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error completing purchase order",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}