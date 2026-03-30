using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class ProductSeed
{
    public static List<ProductV5> GetProducts()
    {
        var now = DateTime.UtcNow;
        var products = new List<ProductV5>();

        void AddProducts(
            string categoryId,
            string categoryName,
            string[] names,
            int baseQuantity,
            decimal baseCost,
            decimal costStep,
            int reorderLevelBase,
            int reorderQtyBase)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var id = $"{categoryId}-{(i + 1):000}";
                var cost = Math.Round(baseCost + (i % 7) * costStep + (i / 7) * 0.10m, 2);
                var price = Math.Round(cost * (categoryName == "Sandwiches" ? 1.95m : 1.75m), 2);

                var quantity = baseQuantity + (i % 5) * 4 + (i % 3) * 2;
                var reorderLevel = reorderLevelBase + (i % 4) * 2;
                var reorderQty = reorderQtyBase + (i % 5) * 3;

                products.Add(new ProductV5
                {
                    Id = id,
                    Pk = "STORE#1",
                    Type = "Product",
                    Name = names[i],
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    Quantity = quantity,
                    Cost = cost,
                    Price = price,
                    ReorderLevel = reorderLevel,
                    ReorderQty = reorderQty,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
        }

        AddProducts(
            "c1", "Sandwiches",
            new[]
            {
                "Ham & Cheese Sandwich",
                "Chicken Mayo Sandwich",
                "Tuna Sweetcorn Sandwich",
                "Turkey & Cheese Sandwich",
                "BLT Sandwich",
                "Egg Mayo Sandwich",
                "Chicken Salad Sandwich",
                "Roast Beef & Cheddar Sandwich",
                "Mozzarella Tomato Pesto Sandwich",
                "Veggie Crunch Sandwich",
                "Coronation Chicken Sandwich",
                "Ham Salad Sandwich",
                "Cheese & Onion Sandwich",
                "Spicy Chicken Sandwich",
                "Chicken Bacon Sandwich",
                "Club Sandwich",
                "Pesto Chicken Sandwich",
                "Southern Fried Chicken Sandwich",
                "Falafel & Hummus Sandwich",
                "Smoked Ham & Mustard Sandwich"
            },
            8, 2.10m, 0.18m, 4, 10
        );

        AddProducts(
            "c2", "Soft Drinks",
            new[]
            {
                "Coca-Cola 330ml",
                "Diet Coke 330ml",
                "Coke Zero 330ml",
                "Pepsi 330ml",
                "Pepsi Max 330ml",
                "7UP 330ml",
                "Sprite 330ml",
                "Fanta Orange 330ml",
                "Fanta Lemon 330ml",
                "Dr Pepper 330ml",
                "Club Orange 500ml",
                "Club Lemon 500ml",
                "Club Rock Shandy 500ml",
                "Lucozade Original 380ml",
                "Lucozade Orange 380ml",
                "Iced Tea Peach 500ml",
                "Iced Tea Lemon 500ml",
                "Ginger Ale 330ml"
            },
            14, 0.70m, 0.08m, 6, 12
        );

        AddProducts(
            "c3", "Energy Drinks",
            new[]
            {
                "Red Bull Original 250ml",
                "Red Bull Sugarfree 250ml",
                "Red Bull Tropical 250ml",
                "Monster Original 500ml",
                "Monster Ultra White 500ml",
                "Monster Mango Loco 500ml",
                "Monster Pipeline Punch 500ml",
                "Rockstar Original 500ml",
                "Rockstar Tropical 500ml",
                "Boost Original 250ml"
            },
            12, 1.10m, 0.14m, 5, 10
        );

        AddProducts(
            "c4", "Water",
            new[]
            {
                "Still Water 500ml",
                "Still Water 750ml",
                "Still Water 1L",
                "Sparkling Water 500ml",
                "Sparkling Water 750ml",
                "Sparkling Water 1L"
            },
            18, 0.35m, 0.06m, 8, 18
        );

        AddProducts(
            "c5", "Fruits",
            new[]
            {
                "Banana Single",
                "Banana Twin Pack",
                "Apple Single",
                "Apple Twin Pack",
                "Pear Single",
                "Pear Twin Pack",
                "Orange Single",
                "Orange Twin Pack",
                "Grapes Snack Pot",
                "Mixed Fruit Pot",
                "Apple Slices Pot",
                "Melon Fruit Pot"
            },
            10, 0.45m, 0.10m, 5, 10
        );

        AddProducts(
            "c6", "Chocolate Bars",
            new[]
            {
                "Snickers Bar",
                "Mars Bar",
                "Twix Bar",
                "KitKat 4 Finger",
                "Galaxy Bar",
                "Dairy Milk Bar",
                "Bounty Bar",
                "Crunchie Bar",
                "Wispa Bar",
                "Kinder Bueno",
                "Lion Bar",
                "Yorkie Bar",
                "Milky Way Bar",
                "Toffee Crisp Bar",
                "Maltesers Bar",
                "Reese's Nutrageous Bar"
            },
            16, 0.55m, 0.07m, 6, 14
        );

        AddProducts(
            "c7", "Protein Bars",
            new[]
            {
                "Protein Bar Chocolate Brownie",
                "Protein Bar Cookies & Cream",
                "Protein Bar Peanut Butter",
                "Protein Bar Salted Caramel",
                "Protein Bar White Chocolate",
                "Protein Bar Double Chocolate",
                "Protein Bar Caramel Nut",
                "Protein Bar Strawberry Yogurt",
                "Protein Bar Vanilla Crisp",
                "Protein Bar Dark Chocolate Mint",
                "Protein Bar Chocolate Orange",
                "Protein Bar Coconut Crunch",
                "Protein Bar Banana Crunch",
                "Protein Bar Hazelnut Nougat",
                "Protein Bar Fudge Brownie",
                "Protein Bar Choco Peanut"
            },
            12, 0.95m, 0.11m, 5, 12
        );

        AddProducts(
            "c8", "Crisps",
            new[]
            {
                "Ready Salted Crisps",
                "Cheese & Onion Crisps",
                "Salt & Vinegar Crisps",
                "Prawn Cocktail Crisps",
                "Smoky Bacon Crisps",
                "Thai Sweet Chilli Crisps",
                "Sour Cream & Onion Crisps",
                "Barbecue Crisps",
                "Sweet Chilli Crisps",
                "Sea Salt & Black Pepper Crisps",
                "Hunky Dory Cheese & Onion",
                "Hunky Dory Salt & Vinegar",
                "Hunky Dory Buffalo",
                "Tayto Cheese & Onion",
                "Tayto Salt & Vinegar",
                "O'Donnell's Mature Cheese",
                "O'Donnell's Ballymaloe Relish",
                "O'Donnell's Sea Salt",
                "Walkers Prawn Cocktail",
                "Walkers Roast Chicken"
            },
            15, 0.50m, 0.08m, 6, 15
        );

        AddProducts(
            "c9", "Nuts",
            new[]
            {
                "Salted Peanuts Pack",
                "Dry Roasted Peanuts Pack",
                "Honey Roasted Peanuts Pack",
                "Cashew Nuts Pack",
                "Almonds Pack",
                "Mixed Nuts Pack",
                "Raisin & Nut Mix Pack",
                "Protein Nut Mix Pack",
                "Chilli Peanuts Pack",
                "Wasabi Peanuts Pack",
                "Pistachios Pack",
                "Walnuts Pack",
                "Hazelnuts Pack",
                "Trail Mix Pack",
                "Deluxe Mixed Nuts Pack",
                "Fruit & Nut Snack Pack"
            },
            11, 0.80m, 0.12m, 5, 10
        );

        AddProducts(
            "c10", "Juices & Smoothies",
            new[]
            {
                "Orange Juice 250ml",
                "Apple Juice 250ml",
                "Multivitamin Juice 250ml",
                "Mango Juice 250ml",
                "Tropical Juice 250ml",
                "Cloudy Apple Juice 500ml",
                "Fresh Orange Juice 500ml",
                "Green Smoothie 250ml",
                "Strawberry Banana Smoothie 250ml",
                "Mango Passion Smoothie 250ml",
                "Berry Blast Smoothie 250ml",
                "Pineapple Juice 250ml",
                "Carrot Orange Juice 250ml",
                "Watermelon Juice 250ml",
                "Apple Berry Smoothie 250ml",
                "Protein Smoothie Chocolate 330ml"
            },
            10, 0.85m, 0.12m, 5, 12
        );

        return products;
    }
}