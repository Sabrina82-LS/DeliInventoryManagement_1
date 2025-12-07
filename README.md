How to Run This Project (No Technical Knowledge Needed)
✔️ 1. Install the Required Tools

Before running the project, install these free tools:

A. Install Software

Visual Studio 2022 Community
Download: https://visualstudio.microsoft.com/

When installing, ensure the workload “ASP.NET and Web Development” is selected.

Azure Cosmos DB Emulator
Download: https://learn.microsoft.com/azure/cosmos-db/local-emulator

B. Install the Required NuGet Packages

These packages are needed for the project to run correctly.

You do NOT need coding skills — just follow the steps:

Open the project in Visual Studio

On the right panel (Solution Explorer), right-click the project DeliInventoryManagement_1.Api

Click Manage NuGet Packages…

Go to the Browse tab

Search for and install:

✔️ Microsoft.Azure.Cosmos

Used to connect to the Cosmos Database.

✔️ Microsoft.AspNetCore.OpenApi

Used for Swagger UI documentation.

No additional setup is needed — Visual Studio will handle the installation for you.

✔️ 2. Start the Cosmos DB Emulator

Open the Azure Cosmos DB Emulator from your desktop or Start Menu.

Wait until its dashboard opens in your browser.

Keep the Emulator running in the background.

✔️ 3. Open and Run the Project

Open the project folder in Visual Studio

Press F5 or click the green ▶️ Run button

A page named Swagger UI will open in your browser

If you see sections for:

Products V1

Categories V2

Suppliers V3

the API is running correctly✨

Testing the System (Using Only Your Browser)

Swagger UI lets you test everything without needing code.

✔️ Products (Version 1)

View products

Add a new product

Update a product

Delete a product

View summary information

✔️ Categories (Version 2)

Add, edit, delete categories

View all categories

View category details

✔️ Suppliers (Version 3)

Add suppliers

Update supplier contact info

Delete suppliers

View all suppliers

All actions are performed using the “Try It Out” button inside Swagger.

How Data Is Stored (Cosmos DB)

The API uses the Cosmos Emulator with:

Database → DeliInventoryDb

Container → InventoryItems

Partition Key → /Type

The API automatically stores:

Products (Type = "Product")

Categories (Type = "Category")

Suppliers (Type = "Supplier")

No manual database setup is needed.

📁 Project Features
✔️ API Versioning

Products → V1

Categories → V2

Suppliers → V3

✔️ Cosmos DB Integration

Documents stored locally in JSON format.

✔️ Sorting, Filtering, Paging

Available in the Products API.

✔️ In-Memory Caching

Improves performance when loading lists.

✔️ Organized Swagger Documentation

Sections appear in this order:

1 - Products V1

2 - Categories V2

3 - Suppliers V3
