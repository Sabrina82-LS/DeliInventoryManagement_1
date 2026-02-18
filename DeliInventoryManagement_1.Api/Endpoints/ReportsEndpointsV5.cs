using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using System.Globalization;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

public static class ReportsEndpointsV5
{
    public static IEndpointRouteBuilder MapReportsV5(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v5/reports")
            .WithTags("Reports V5");

        // GET /api/v5/reports/sales?from=2026-02-01&to=2026-02-28&pk=STORE#1
        group.MapGet("/sales", async (
            DateTime? from,
            DateTime? to,
            string? pk,
            CosmosClient cosmos,
            IConfiguration config) =>
        {
            var (dbId, containerId, pkValue, partitionKey) = GetCosmosInfo(config, pk);
            var container = cosmos.GetContainer(dbId, containerId);

            var q = BuildSalesQuery(from, to);

            var docs = await ReadAll<SaleDoc>(container, q, partitionKey);

            var report = docs
                .OrderByDescending(d => d.Date)
                .SelectMany(d => (d.Lines ?? new List<SaleLine>()).Select(line => new SaleReportItemDto
                {
                    Id = d.Id ?? "",
                    Date = d.Date,
                    ProductId = line.ProductId ?? "",
                    ProductName = line.ProductName ?? "",
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Total = line.UnitPrice * line.Quantity
                }))
                .ToList();

            return Results.Ok(report);
        });

        // GET /api/v5/reports/restocks?from=2026-02-01&to=2026-02-28&pk=STORE#1
        group.MapGet("/restocks", async (
            DateTime? from,
            DateTime? to,
            string? pk,
            CosmosClient cosmos,
            IConfiguration config) =>
        {
            var (dbId, containerId, pkValue, partitionKey) = GetCosmosInfo(config, pk);
            var container = cosmos.GetContainer(dbId, containerId);

            // ✅ teu Restock no Cosmos está em PascalCase: Date, SupplierId, SupplierName, Lines, Quantity, CostPerUnit...
            // então a leitura e filtro por data precisam mirar nesses nomes.
            var q = BuildRestocksQuery(from, to);

            var docs = await ReadAll<RestockDoc>(container, q, partitionKey);

            var report = docs
                .OrderByDescending(d => d.Date)
                .SelectMany(d => (d.Lines ?? new List<RestockLine>()).Select(line => new RestockReportItemDto
                {
                    Id = d.Id ?? "",
                    Date = d.Date,

                    SupplierId = d.SupplierId ?? "",
                    SupplierName = d.SupplierName ?? "",

                    ProductId = line.ProductId ?? "",
                    ProductName = line.ProductName ?? "",

                    QuantityAdded = line.Quantity,          // ✅ no teu JSON é "Quantity"
                    CostPerUnit = line.CostPerUnit,
                    LineTotal = line.LineTotal
                }))
                .ToList();

            return Results.Ok(report);
        });

        return app;
    }

    // =========================
    // DTOs (Blazor consome)
    // =========================
    public sealed class SaleReportItemDto
    {
        public string Id { get; set; } = "";
        public DateTime Date { get; set; }
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class RestockReportItemDto
    {
        public string Id { get; set; } = "";
        public DateTime Date { get; set; }
        public string SupplierId { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int QuantityAdded { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal LineTotal { get; set; }
    }

    // =========================
    // Cosmos docs
    // =========================

    // ✅ Sale no teu Cosmos está camelCase (date/lines/total)
    private sealed class SaleDoc
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("pk")] public string? Pk { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }

        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("lines")] public List<SaleLine>? Lines { get; set; }
        [JsonPropertyName("total")] public decimal Total { get; set; }
    }

    private sealed class SaleLine
    {
        [JsonPropertyName("productId")] public string? ProductId { get; set; }
        [JsonPropertyName("productName")] public string? ProductName { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("unitPrice")] public decimal UnitPrice { get; set; }
    }

    // ✅ Restock no teu Cosmos está PascalCase (Date/Lines/Quantity/CostPerUnit/LineTotal)
    // (isso explica por que estava vindo [] quando você filtrava por c.date)
    private sealed class RestockDoc
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("pk")] public string? Pk { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }

        [JsonPropertyName("Date")] public DateTime Date { get; set; }

        [JsonPropertyName("SupplierId")] public string? SupplierId { get; set; }
        [JsonPropertyName("SupplierName")] public string? SupplierName { get; set; }

        [JsonPropertyName("Lines")] public List<RestockLine>? Lines { get; set; }

        [JsonPropertyName("TotalCost")] public decimal TotalCost { get; set; }
    }

    private sealed class RestockLine
    {
        [JsonPropertyName("ProductId")] public string? ProductId { get; set; }
        [JsonPropertyName("ProductName")] public string? ProductName { get; set; }

        [JsonPropertyName("Quantity")] public int Quantity { get; set; }          // ✅
        [JsonPropertyName("CostPerUnit")] public decimal CostPerUnit { get; set; } // ✅
        [JsonPropertyName("LineTotal")] public decimal LineTotal { get; set; }     // ✅
    }

    // =========================
    // Helpers
    // =========================
    private static (string dbId, string containerId, string pkValue, PartitionKey pk) GetCosmosInfo(IConfiguration config, string? pk)
    {
        var dbId = config["Cosmos:DatabaseId"] ?? "DeliInventoryDb";
        var containerId = config["Cosmos:OperationsContainerId"] ?? "Operations";
        var pkValue = string.IsNullOrWhiteSpace(pk) ? "STORE#1" : pk.Trim();
        return (dbId, containerId, pkValue, new PartitionKey(pkValue));
    }

    private static QueryDefinition BuildSalesQuery(DateTime? from, DateTime? to)
    {
        // ✅ Sales: date (camelCase)
        var sql = "SELECT * FROM c WHERE c.type = @type";

        if (from.HasValue)
            sql += " AND c.date >= @from";

        if (to.HasValue)
            sql += " AND c.date <= @to";

        var q = new QueryDefinition(sql)
            .WithParameter("@type", "Sale");

        if (from.HasValue)
            q = q.WithParameter("@from", ToIsoZ(from.Value));

        if (to.HasValue)
            q = q.WithParameter("@to", ToIsoZ(to.Value.Date.AddDays(1).AddTicks(-1)));

        return q;
    }

    private static QueryDefinition BuildRestocksQuery(DateTime? from, DateTime? to)
    {
        // ✅ Restocks: Date (PascalCase) — conforme teu Cosmos print
        var sql = "SELECT * FROM c WHERE c.type = @type";

        if (from.HasValue)
            sql += " AND c.Date >= @from";

        if (to.HasValue)
            sql += " AND c.Date <= @to";

        var q = new QueryDefinition(sql)
            .WithParameter("@type", "Restock");

        if (from.HasValue)
            q = q.WithParameter("@from", ToIsoZ(from.Value));

        if (to.HasValue)
            q = q.WithParameter("@to", ToIsoZ(to.Value.Date.AddDays(1).AddTicks(-1)));

        return q;
    }

    private static async Task<List<T>> ReadAll<T>(Container container, QueryDefinition query, PartitionKey pk)
    {
        var results = new List<T>();

        using var iterator = container.GetItemQueryIterator<T>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = pk }
        );

        while (iterator.HasMoreResults)
        {
            var resp = await iterator.ReadNextAsync();
            results.AddRange(resp);
        }

        return results;
    }

    private static string ToIsoZ(DateTime dtUtc)
    {
        var utc = dtUtc.Kind == DateTimeKind.Utc ? dtUtc : dtUtc.ToUniversalTime();
        return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }
}
