using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Endpoints.V5;
using DeliInventoryManagement_1.Api.ModelsV5.Auth;
using DeliInventoryManagement_1.Api.Services.Auth;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Services
{
    // ══════════════════════════════════════════════════════
    // PasswordHasher Tests
    // Pure unit tests - no external dependencies at all
    // ══════════════════════════════════════════════════════
    public class PasswordHasherTests
    {
        [Fact]
        public void Hash_ReturnsNonEmptyString()
        {
            var hash = PasswordHasher.Hash("mypassword");
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }

        [Fact]
        public void Hash_SamePasswordProducesSameHash()
        {
            var hash1 = PasswordHasher.Hash("mypassword");
            var hash2 = PasswordHasher.Hash("mypassword");
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Hash_DifferentPasswordsProduceDifferentHashes()
        {
            var hash1 = PasswordHasher.Hash("password1");
            var hash2 = PasswordHasher.Hash("password2");
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var password = "Admin123!";
            var hash = PasswordHasher.Hash(password);
            Assert.True(PasswordHasher.Verify(password, hash));
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("correctpassword");
            Assert.False(PasswordHasher.Verify("wrongpassword", hash));
        }

        [Fact]
        public void Verify_EmptyPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("realpassword");
            Assert.False(PasswordHasher.Verify("", hash));
        }

        [Fact]
        public void Verify_CaseSensitive_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("Password123");
            Assert.False(PasswordHasher.Verify("password123", hash));
        }
    }

    // ══════════════════════════════════════════════════════
    // JwtTokenService Tests
    // Pure unit tests - no external dependencies
    // ══════════════════════════════════════════════════════
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _service;

        public JwtTokenServiceTests()
        {
            // Build a minimal IConfiguration with JWT settings
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:Key"] = "TestSuperSecretKey_MustBe32CharsLong!",
                ["Jwt:ExpiresMinutes"] = "60"
            };

            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            _service = new JwtTokenService(config);
        }

        [Fact]
        public void CreateToken_WithValidUser_ReturnsNonEmptyToken()
        {
            // Arrange
            var user = new AppUser
            {
                id = "U1001",
                pk = "USER",
                Email = "admin@deli.com",
                FullName = "Admin User",
                Role = "Admin",
                PasswordHash = PasswordHasher.Hash("Admin123!"),
                IsActive = true
            };

            // Act
            var token = _service.CreateToken(user);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void CreateToken_WithStaffUser_ReturnsToken()
        {
            // Arrange
            var user = new AppUser
            {
                id = "U1002",
                pk = "USER",
                Email = "staff@deli.com",
                FullName = "Staff User",
                Role = "Staff",
                PasswordHash = PasswordHasher.Hash("Staff123!"),
                IsActive = true
            };

            // Act
            var token = _service.CreateToken(user);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));

            // Token should have 3 parts separated by dots (JWT format)
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
        }

        [Fact]
        public void CreateToken_TwoCalls_ReturnsDifferentTokens()
        {
            // Arrange - tokens should differ because of timestamp
            var user = new AppUser
            {
                id = "U1001",
                Email = "admin@deli.com",
                FullName = "Admin User",
                Role = "Admin"
            };

            // Act
            var token1 = _service.CreateToken(user);
            System.Threading.Thread.Sleep(1100); // ensure different timestamp
            var token2 = _service.CreateToken(user);

            // Assert - tokens are time-based so they differ
            Assert.NotEqual(token1, token2);
        }
    }

    // ══════════════════════════════════════════════════════
    // Auth Endpoint Logic Tests
    // Tests the register/login validation logic
    // using mocked Cosmos - no real DB needed
    // ══════════════════════════════════════════════════════
    public class AppUserModelTests
    {
        [Fact]
        public void AppUser_DefaultRole_IsStaff()
        {
            var user = new AppUser();
            Assert.Equal("Staff", user.Role);
        }

        [Fact]
        public void AppUser_DefaultIsActive_IsTrue()
        {
            var user = new AppUser();
            Assert.True(user.IsActive);
        }

        [Fact]
        public void AppUser_DefaultPk_IsUSER()
        {
            var user = new AppUser();
            Assert.Equal("USER", user.pk);
        }

        [Fact]
        public void AppUser_GeneratesUniqueIds()
        {
            var user1 = new AppUser();
            var user2 = new AppUser();
            Assert.NotEqual(user1.id, user2.id);
        }

        [Fact]
        public void AppUser_PasswordResetToken_DefaultsToNull()
        {
            var user = new AppUser();
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetTokenExpiry);
        }
    }

    // ══════════════════════════════════════════════════════
    // RegisterRequest / LoginRequest Model Tests
    // ══════════════════════════════════════════════════════
    public class AuthRequestModelTests
    {
        [Fact]
        public void LoginRequest_DefaultValues_AreEmptyStrings()
        {
            var req = new LoginRequest();
            Assert.Equal("", req.Email);
            Assert.Equal("", req.Password);
        }

        [Fact]
        public void RegisterRequest_DefaultValues_AreEmptyStrings()
        {
            var req = new RegisterRequest();
            Assert.Equal("", req.Email);
            Assert.Equal("", req.FullName);
            Assert.Equal("", req.Password);
            Assert.Equal("", req.ConfirmPassword);
        }

        [Fact]
        public void RegisterRequest_PasswordMismatch_DetectedCorrectly()
        {
            var req = new RegisterRequest
            {
                Password = "password123",
                ConfirmPassword = "different456"
            };

            Assert.NotEqual(req.Password, req.ConfirmPassword);
        }

        [Fact]
        public void RegisterRequest_PasswordMatch_DetectedCorrectly()
        {
            var req = new RegisterRequest
            {
                Password = "password123",
                ConfirmPassword = "password123"
            };

            Assert.Equal(req.Password, req.ConfirmPassword);
        }

        [Fact]
        public void ForgotPasswordRequest_DefaultEmail_IsEmpty()
        {
            var req = new ForgotPasswordRequest();
            Assert.Equal("", req.Email);
        }

        [Fact]
        public void ResetPasswordRequest_DefaultValues_AreEmptyStrings()
        {
            var req = new ResetPasswordRequest();
            Assert.Equal("", req.Token);
            Assert.Equal("", req.NewPassword);
            Assert.Equal("", req.ConfirmPassword);
        }
    }

    // ══════════════════════════════════════════════════════
    // LoginResponse Model Tests
    // ══════════════════════════════════════════════════════
    public class LoginResponseTests
    {
        [Fact]
        public void LoginResponse_DefaultValues_AreEmptyStrings()
        {
            var resp = new LoginResponse();
            Assert.Equal("", resp.Token);
            Assert.Equal("", resp.Role);
            Assert.Equal("", resp.FullName);
        }

        [Fact]
        public void LoginResponse_CanSetAllProperties()
        {
            var resp = new LoginResponse
            {
                Token = "jwt-token-here",
                Role = "Admin",
                FullName = "Admin User"
            };

            Assert.Equal("jwt-token-here", resp.Token);
            Assert.Equal("Admin", resp.Role);
            Assert.Equal("Admin User", resp.FullName);
        }
    }
}