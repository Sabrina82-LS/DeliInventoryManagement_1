using DeliInventoryManagement_1.Api.ModelsV5.Auth;
using DeliInventoryManagement_1.Api.Services.Auth;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class UserSeed
{
    public static async Task EnsureUsersAsync(CosmosClient cosmos, IConfiguration config)
    {
        var dbId = config["CosmosDb:DatabaseId"] ?? "DeliInventoryDb";
        var usersContainerId = config["CosmosDb:Containers:Users"] ?? "Users";
        var container = cosmos.GetContainer(dbId, usersContainerId);

        // ✅ IDs simples e fáceis de lembrar
        await EnsureUser(container, id: "U1001", email: "admin@deli.com", name: "Admin User", role: "Admin", password: "Admin123!");
        await EnsureUser(container, id: "U1002", email: "staff@deli.com", name: "Staff User", role: "Staff", password: "Staff123!");
    }

    private static async Task EnsureUser(
        Container container,
        string id,
        string email,
        string name,
        string role,
        string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // 1) Verifica se já existe o email
        var qEmail = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.pk = 'USER' AND LOWER(c.email) = @email")
            .WithParameter("@email", normalizedEmail);

        using var feedEmail = container.GetItemQueryIterator<AppUser>(qEmail);
        var pageEmail = await feedEmail.ReadNextAsync();
        if (pageEmail.Resource.Any())
            return;

        // 2) (Extra) Verifica se já existe o ID (pra evitar conflito)
        var qId = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.pk = 'USER' AND c.id = @id")
            .WithParameter("@id", id);

        using var feedId = container.GetItemQueryIterator<AppUser>(qId);
        var pageId = await feedId.ReadNextAsync();
        if (pageId.Resource.Any())
            return;

        // 3) Cria o user com ID simples
        var user = new AppUser
        {
            id = id,                 // ✅ ID simples (U1001, U1002...)
            pk = "USER",             // ✅ partition key do container Users (/pk)
            Email = normalizedEmail,
            FullName = name.Trim(),
            Role = role,
            PasswordHash = PasswordHasher.Hash(password),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await container.CreateItemAsync(user, new PartitionKey(user.pk));
    }
}