using DeliInventoryManagement_1.Api.Tests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DeliInventoryManagement_1.Api.Tests.Infrastructure
{
    /// <summary>
    /// Creates a real in-memory test server using the actual API Program class.
    /// - Replaces JWT authentication with a test handler (no real token needed)
    /// - Uses "Testing" environment so seed and Cosmos bootstrap are skipped
    /// - Integration tests that need real Cosmos should still be marked [Fact(Skip = "CI")]
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Use "Testing" environment - Program.cs already checks for this
            // and skips EnsureCosmosSchemaAsync, EnsureSeedUsersAsync, SeedRunnerV5
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Replace real JWT auth with a test handler that always
                // returns an authenticated Admin user - no token needed in tests
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });
            });
        }
    }
}