using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5.Auth;
using DeliInventoryManagement_1.Api.Services.Auth;
//using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

//public static class AuthEndpointsV5
//{
//    public static IEndpointRouteBuilder MapAuthV5(this IEndpointRouteBuilder app)
//    {
//        var group = app.MapGroup("/api/v5/auth")
//            .WithTags("Auth V5")
//            .AllowAnonymous();

//        group.MapPost("/login", async (
//            LoginRequest req,
//            CosmosClient cosmos,
//            IOptions<CosmosOptions> cosmosOpt,
//            JwtTokenService jwt) =>
//        {
//            var dbId = cosmosOpt.Value.DatabaseId;

//            // ✅ usa o container configurado (se existir) e fallback para "Users"
//            var usersContainerId = cosmosOpt.Value.Containers?.Users ?? "Users";
//            var container = cosmos.GetContainer(dbId, usersContainerId);

//            var email = (req.Email ?? "").Trim().ToLowerInvariant();

//            // ✅ CORREÇÃO: no Cosmos seu documento está com "Email" (E maiúsculo),
//            // então a query precisa usar c.Email (case-sensitive)
//            var q = new QueryDefinition(
//                "SELECT TOP 1 * FROM c WHERE c.pk = 'USER' AND LOWER(c.Email) = @email")
//                .WithParameter("@email", email);

//            using var feed = container.GetItemQueryIterator<AppUser>(q);
//            var page = await feed.ReadNextAsync();
//            var user = page.Resource.FirstOrDefault();

//            if (user is null || !user.IsActive)
//                return Results.Unauthorized();

//            if (!PasswordHasher.Verify(req.Password, user.PasswordHash))
//                return Results.Unauthorized();

//            var token = jwt.CreateToken(user);

//            return Results.Ok(new LoginResponse
//            {
//                Token = token,
//                Role = user.Role,
//                FullName = user.FullName
//            });
//        })
//        .AllowAnonymous();

//        return app;
//    }
//}


public static class AuthEndpointsV5
{
    public static IEndpointRouteBuilder MapAuthV5(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v5/auth")
            .WithTags("Auth V5")
            .AllowAnonymous();

        // =====================================================
        // POST /api/v5/auth/login
        // =====================================================
        group.MapPost("/login", async (
            LoginRequest req,
            CosmosClient cosmos,
            IOptions<CosmosOptions> cosmosOpt,
            JwtTokenService jwt) =>
        {
            var container = GetUsersContainer(cosmos, cosmosOpt.Value);
            var email = (req.Email ?? "").Trim().ToLowerInvariant();

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

        // =====================================================
        // POST /api/v5/auth/register
        // Public - creates a Staff user
        // =====================================================
        group.MapPost("/register", async (
            RegisterRequest req,
            CosmosClient cosmos,
            IOptions<CosmosOptions> cosmosOpt) =>
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(req.FullName))
                return Results.BadRequest(new { message = "Full name is required." });

            if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
                return Results.BadRequest(new { message = "Password must be at least 6 characters." });

            if (req.Password != req.ConfirmPassword)
                return Results.BadRequest(new { message = "Passwords do not match." });

            var container = GetUsersContainer(cosmos, cosmosOpt.Value);
            var email = req.Email.Trim().ToLowerInvariant();

            // Check if email already exists
            var checkQ = new QueryDefinition(
                "SELECT TOP 1 c.id FROM c WHERE c.pk = 'USER' AND LOWER(c.Email) = @email")
                .WithParameter("@email", email);

            using var checkFeed = container.GetItemQueryIterator<dynamic>(checkQ);
            var checkPage = await checkFeed.ReadNextAsync();

            if (checkPage.Resource.Any())
                return Results.Conflict(new { message = "An account with this email already exists." });

            // Create the user as Staff (never Admin via public register)
            var user = new AppUser
            {
                pk = "USER",
                Email = email,
                FullName = req.FullName.Trim(),
                Role = "Staff",
                PasswordHash = PasswordHasher.Hash(req.Password),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await container.CreateItemAsync(user, new PartitionKey(user.pk));

            return Results.Created($"/api/v5/users/{user.id}", new
            {
                user.id,
                user.Email,
                user.FullName,
                user.Role,
                message = "Account created successfully. You can now log in."
            });
        })
        .AllowAnonymous();

        // =====================================================
        // POST /api/v5/auth/forgot-password
        // Public - stores a reset token, returns it (in real
        // app you'd email it; for now we return it directly
        // so you can test without an email service)
        // =====================================================
        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest req,
            CosmosClient cosmos,
            IOptions<CosmosOptions> cosmosOpt) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { message = "Email is required." });

            var container = GetUsersContainer(cosmos, cosmosOpt.Value);
            var email = req.Email.Trim().ToLowerInvariant();

            var q = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.pk = 'USER' AND LOWER(c.Email) = @email")
                .WithParameter("@email", email);

            using var feed = container.GetItemQueryIterator<AppUser>(q);
            var page = await feed.ReadNextAsync();
            var user = page.Resource.FirstOrDefault();

            // Always return OK - don't reveal if email exists (security best practice)
            if (user is null || !user.IsActive)
                return Results.Ok(new { message = "If that email exists, a reset link has been sent." });

            // Generate a simple reset token (GUID) + expiry
            var resetToken = Guid.NewGuid().ToString("N");
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.pk));

            // In production: send email with reset link
            // For now: return token so you can test with Swagger
            return Results.Ok(new
            {
                message = "If that email exists, a reset link has been sent.",
                // Remove the line below in production - only for dev/testing
                devResetToken = resetToken
            });
        })
        .AllowAnonymous();

        // =====================================================
        // POST /api/v5/auth/reset-password
        // Public - validates token and sets new password
        // =====================================================
        group.MapPost("/reset-password", async (
            ResetPasswordRequest req,
            CosmosClient cosmos,
            IOptions<CosmosOptions> cosmosOpt) =>
        {
            if (string.IsNullOrWhiteSpace(req.Token))
                return Results.BadRequest(new { message = "Reset token is required." });

            if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
                return Results.BadRequest(new { message = "Password must be at least 6 characters." });

            if (req.NewPassword != req.ConfirmPassword)
                return Results.BadRequest(new { message = "Passwords do not match." });

            var container = GetUsersContainer(cosmos, cosmosOpt.Value);

            var q = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.pk = 'USER' AND c.PasswordResetToken = @token")
                .WithParameter("@token", req.Token);

            using var feed = container.GetItemQueryIterator<AppUser>(q);
            var page = await feed.ReadNextAsync();
            var user = page.Resource.FirstOrDefault();

            if (user is null)
                return Results.BadRequest(new { message = "Invalid or expired reset token." });

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return Results.BadRequest(new { message = "Reset token has expired. Please request a new one." });

            // Update password and clear the token
            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await container.ReplaceItemAsync(user, user.id, new PartitionKey(user.pk));

            return Results.Ok(new { message = "Password reset successfully. You can now log in." });
        })
        .AllowAnonymous();

        return app;
    }

    // -------------------------
    // Helper
    // -------------------------
    private static Container GetUsersContainer(CosmosClient cosmos, CosmosOptions opt)
    {
        var usersContainerId = opt.Containers?.Users ?? "Users";
        return cosmos.GetContainer(opt.DatabaseId, usersContainerId);
    }
}
