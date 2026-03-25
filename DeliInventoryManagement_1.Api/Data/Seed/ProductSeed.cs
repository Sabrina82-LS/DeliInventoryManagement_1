using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class ProductSeed
{
    public static List<ProductV5> GetProducts()
    {
        var now = DateTime.UtcNow;
        var products = new List<ProductV5>();

        // =========================================================
        // Helper
        // =========================================================
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

                // Variação controlada para criar cenário realista de stock e reposição semanal
                var quantity = baseQuantity + (i % 5) * 4 + (i % 3) * 2;
                var reorderLevel = reorderLevelBase + (i % 4) * 2;   // ex: 4,6,8,10...
                var reorderQty = reorderQtyBase + (i % 5) * 3;       // ex: 8,11,14,17...

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

        // =========================================================
        // c1 - Sandwiches (20)
        // =========================================================
        AddProducts(
            categoryId: "c1",
            categoryName: "Sandwiches",
            names: new[]
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
            baseQuantity: 8,
            baseCost: 2.10m,
            costStep: 0.18m,
            reorderLevelBase: 4,
            reorderQtyBase: 10
        );

        // =========================================================
        // c2 - Soft Drinks (18)
        // =========================================================
        AddProducts(
            categoryId: "c2",
            categoryName: "Soft Drinks",
            names: new[]
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
            baseQuantity: 14,
            baseCost: 0.70m,
            costStep: 0.08m,
            reorderLevelBase: 6,
            reorderQtyBase: 12
        );

        // =========================================================
        // c3 - Energy Drinks (10)
        // =========================================================
        AddProducts(
            categoryId: "c3",
            categoryName: "Energy Drinks",
            names: new[]
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
            baseQuantity: 12,
            baseCost: 1.10m,
            costStep: 0.14m,
            reorderLevelBase: 5,
            reorderQtyBase: 10
        );

        // =========================================================
        // c4 - Water (6) -> 2 tipos de água em garrafa, em variações
        // =========================================================
        AddProducts(
            categoryId: "c4",
            categoryName: "Water",
            names: new[]
            {
                "Still Water 500ml",
                "Still Water 750ml",
                "Still Water 1L",
                "Sparkling Water 500ml",
                "Sparkling Water 750ml",
                "Sparkling Water 1L"
            },
            baseQuantity: 18,
            baseCost: 0.35m,
            costStep: 0.06m,
            reorderLevelBase: 8,
            reorderQtyBase: 18
        );

        // =========================================================
        // c5 - Fruits (12)
        // =========================================================
        AddProducts(
            categoryId: "c5",
            categoryName: "Fruits",
            names: new[]
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
            baseQuantity: 10,
            baseCost: 0.45m,
            costStep: 0.10m,
            reorderLevelBase: 5,
            reorderQtyBase: 10
        );

        // =========================================================
        // c6 - Chocolate Bars (16)
        // =========================================================
        AddProducts(
            categoryId: "c6",
            categoryName: "Chocolate Bars",
            names: new[]
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
            baseQuantity: 16,
            baseCost: 0.55m,
            costStep: 0.07m,
            reorderLevelBase: 6,
            reorderQtyBase: 14
        );

        // =========================================================
        // c7 - Protein Bars (16)
        // =========================================================
        AddProducts(
            categoryId: "c7",
            categoryName: "Protein Bars",
            names: new[]
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
            baseQuantity: 12,
            baseCost: 0.95m,
            costStep: 0.11m,
            reorderLevelBase: 5,
            reorderQtyBase: 12
        );

        // =========================================================
        // c8 - Crisps (20)
        // =========================================================
        AddProducts(
            categoryId: "c8",
            categoryName: "Crisps",
            names: new[]
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
            baseQuantity: 15,
            baseCost: 0.50m,
            costStep: 0.08m,
            reorderLevelBase: 6,
            reorderQtyBase: 15
        );

        // =========================================================
        // c9 - Nuts (16)
        // =========================================================
        AddProducts(
            categoryId: "c9",
            categoryName: "Nuts",
            names: new[]
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
            baseQuantity: 11,
            baseCost: 0.80m,
            costStep: 0.12m,
            reorderLevelBase: 5,
            reorderQtyBase: 10
        );

        // =========================================================
        // c10 - Juices & Smoothies (16)
        // =========================================================
        AddProducts(
            categoryId: "c10",
            categoryName: "Juices & Smoothies",
            names: new[]
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
            baseQuantity: 10,
            baseCost: 0.85m,
            costStep: 0.12m,
            reorderLevelBase: 5,
            reorderQtyBase: 12
        );

        return products;
    }
}