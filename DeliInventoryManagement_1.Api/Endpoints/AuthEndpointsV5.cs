using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5.Auth;
using DeliInventoryManagement_1.Api.Services.Auth;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

public static class AuthEndpointsV5
{
    public static IEndpointRouteBuilder MapAuthV5(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v5/auth")
            .WithTags("Auth V5");

        group.MapPost("/login", async (
            LoginRequest req,
            CosmosClient cosmos,
            IOptions<CosmosOptions> cosmosOpt,
            JwtTokenService jwt) =>
        {
            var dbId = cosmosOpt.Value.DatabaseId;

            // ✅ usa o container configurado (se existir) e fallback para "Users"
            var usersContainerId = cosmosOpt.Value.Containers?.Users ?? "Users";
            var container = cosmos.GetContainer(dbId, usersContainerId);

            var email = (req.Email ?? "").Trim().ToLowerInvariant();

            // ✅ CORREÇÃO: no Cosmos seu documento está com "Email" (E maiúsculo),
            // então a query precisa usar c.Email (case-sensitive)
            var q = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.pk = 'USER' AND LOWER(c.Email) = @email")
                .WithParameter("@email", email);

            using var feed = container.GetItemQueryIterator<AppUser>(q);
            var page = await feed.ReadNextAsync();
            var user = page.Resource.FirstOrDefault();

            if (user is null || !user.IsActive)
                return Results.Unauthorized();

            if (!PasswordHasher.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var token = jwt.CreateToken(user);

            return Results.Ok(new LoginResponse
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName
            });
        })
        .AllowAnonymous();

        return app;
    }
}