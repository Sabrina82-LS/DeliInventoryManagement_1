using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class SupplierSeed
{
    public static List<Supplier> GetSuppliers()
    {
        return new List<Supplier>
        {
               new Supplier
                {
                    Id = "s1",
                    Type = "Supplier",
                    Name = "Tesco Ireland",
                    ContactName = "Procurement Department",
                    Phone = "+353 1 215 0200",
                    Email = "procurement@tesco.ie",
                    Address = "Dublin, Ireland",
                    Notes = "Main grocery supplier"
                },
                new Supplier
                {
                    Id = "s2",
                    Type = "Supplier",
                    Name = "Dunnes Stores",
                    ContactName = "Commercial Team",
                    Phone = "+353 1 844 5000",
                    Email = "suppliers@dunnesstores.ie",
                    Address = "Dublin, Ireland",
                    Notes = "Retail and wholesale supplier"
                },
                new Supplier
                {
                    Id = "s3",
                    Type = "Supplier",
                    Name = "SuperValu",
                    ContactName = "Distribution Centre",
                    Phone = "+353 1 204 6000",
                    Email = "distribution@supervalu.ie",
                    Address = "Cork, Ireland",
                    Notes = "Twice-weekly deliveries"
                },
                new Supplier
                {
                    Id = "s4",
                    Type = "Supplier",
                    Name = "Musgrave Wholesale",
                    ContactName = "Wholesale Operations",
                    Phone = "+353 21 452 2100",
                    Email = "wholesale@musgrave.ie",
                    Address = "Cork, Ireland",
                    Notes = "48h average delivery time"
                },
                new Supplier
                {
                    Id = "s5",
                    Type = "Supplier",
                    Name = "BWG Foods",
                    ContactName = "Sales Department",
                    Phone = "+353 1 409 0400",
                    Email = "sales@bwgfoods.ie",
                    Address = "Dublin, Ireland",
                    Notes = "Minimum order €50"
                },
                new Supplier
                {
                    Id = "s6",
                    Type = "Supplier",
                    Name = "Lidl Ireland",
                    ContactName = "Supplier Management",
                    Phone = "+353 1 920 0000",
                    Email = "suppliers@lidl.ie",
                    Address = "Tallaght, Dublin, Ireland",
                    Notes = "Fixed delivery routes"
                }

        };
    }
}
