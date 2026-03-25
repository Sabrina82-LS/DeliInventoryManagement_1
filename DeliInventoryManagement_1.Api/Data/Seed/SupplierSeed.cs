using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class SupplierSeed
{
    public static List<SupplierV5> GetSuppliers()
    {
        return new List<SupplierV5>
        {
            new SupplierV5
            {
                Id = "s1",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "Tesco Ireland",
                Phone = "+353 1 215 0200",
                Email = "procurement@tesco.ie",
                Notes = "Main grocery supplier",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new SupplierV5
            {
                Id = "s2",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "Dunnes Stores",
                Phone = "+353 1 844 5000",
                Email = "suppliers@dunnesstores.ie",
                Notes = "Retail and wholesale supplier",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new SupplierV5
            {
                Id = "s3",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "SuperValu",
                Phone = "+353 1 204 6000",
                Email = "distribution@supervalu.ie",
                Notes = "Twice-weekly deliveries",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new SupplierV5
            {
                Id = "s4",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "Musgrave Wholesale",
                Phone = "+353 21 452 2100",
                Email = "wholesale@musgrave.ie",
                Notes = "48h average delivery time",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new SupplierV5
            {
                Id = "s5",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "BWG Foods",
                Phone = "+353 1 409 0400",
                Email = "sales@bwgfoods.ie",
                Notes = "Minimum order €50",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new SupplierV5
            {
                Id = "s6",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "Lidl Ireland",
                Phone = "+353 1 920 0000",
                Email = "suppliers@lidl.ie",
                Notes = "Fixed delivery routes",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };
    }
}