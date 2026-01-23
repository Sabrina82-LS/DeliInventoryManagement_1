using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class CategorySeed
{
    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new Category
            {
                Id = "c1",
                Type = "Category",
                Name = "Meat",
                Description = "Fresh and frozen meat products"
            },
            new Category
            {
                Id = "c2",
                Type = "Category",
                Name = "Dairy",
                Description = "Milk, cheese, yogurt and other dairy products"
            },
            new Category
            {
                Id = "c3",
                Type = "Category",
                Name = "Beverages",
                Description = "Soft drinks, juices, water and beverages"
            },
            new Category
            {
                Id = "c4",
                Type = "Category",
                Name = "Bakery",
                Description = "Bread, cakes and baked goods"
            },
            new Category
            {
                Id = "c5",
                Type = "Category",
                Name = "Vegetables",
                Description = "Fresh vegetables and greens"
            },
            new Category
            {
                Id = "c6",
                Type = "Category",
                Name = "Fruits",
                Description = "Fresh seasonal fruits"
            },
            new Category
            {
                Id = "c7",
                Type = "Category",
                Name = "Frozen",
                Description = "Frozen foods and ready meals"
            }
        };
    }
}
