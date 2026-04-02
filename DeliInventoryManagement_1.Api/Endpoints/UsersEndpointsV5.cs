using DeliInventoryManagement_1.Api.ModelsV5.Auth;
using DeliInventoryManagement_1.Api.Services.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

public static class UsersEndpointsV5
{
    public static IEndpointRouteBuilder MapUsersV5(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v5/users")
            .WithTags("Users V5")
            .RequireAuthorization();// temporário para teste

        group.MapPost("", async (
            CreateUserRequest req,
            CosmosClient cosmos,
            IConfiguration config) =>
        {
            var role = req.Role is "Admin" or "Staff" ? req.Role : "Staff";

            var (dbId, usersContainerId) = GetCosmosUsersInfo(config);
            var container = cosmos.GetContainer(dbId, usersContainerId);

            var email = req.Email.Trim().ToLowerInvariant();

            var q = new QueryDefinition(
                "SELECT TOP 1 c.id FROM c WHERE c.pk = 'USER' AND LOWER(c.Email) = @email")
                .WithParameter("@email", email);

            using var feed = container.GetItemQueryIterator<dynamic>(q);
            var page = await feed.ReadNextAsync();

            if (page.Resource.Any())
                return Results.Conflict(new { message = "Email already exists." });

            var user = new AppUser
            {
                pk = "USER",
                Email = email,
                FullName = req.FullName.Trim(),
                Role = role,
                PasswordHash = PasswordHasher.Hash(req.Password),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await container.CreateItemAsync(user, new PartitionKey(user.pk));

            return Results.Created(
                $"/api/v5/users/{user.id}",
                new
                {
                    user.id,
                    user.Email,
                    user.FullName,
                    user.Role,
                    user.IsActive,
                    user.CreatedAtUtc
                });
        });

        group.MapGet("", async (CosmosClient cosmos, IConfiguration config) =>
        {
            var (dbId, usersContainerId) = GetCosmosUsersInfo(config);
            var container = cosmos.GetContainer(dbId, usersContainerId);

            var q = new QueryDefinition(
                "SELECT c.id, c.Email, c.FullName, c.Role, c.IsActive, c.CreatedAtUtc FROM c WHERE c.pk = 'USER'");

            using var feed = container.GetItemQueryIterator<dynamic>(q);

            var list = new List<dynamic>();
            while (feed.HasMoreResults)
            {
                var page = await feed.ReadNextAsync();
                list.AddRange(page.Resource);
            }

            return Results.Ok(list);
        });

        return app;
    }

    private static (string dbId, string usersContainerId) GetCosmosUsersInfo(IConfiguration config)
    {
        var dbId = config["CosmosDb:DatabaseId"] ?? "DeliInventoryDb";
        var usersContainerId = config["CosmosDb:Containers:Users"] ?? "Users";
        return (dbId, usersContainerId);
    }
}