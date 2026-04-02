using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DeliInventoryManagement_1.Blazor.Services.Auth;

public sealed class AuthService
{
    private const string TokenKey = "auth_token";
    private const string RoleKey = "auth_role";
    private const string NameKey = "auth_name";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ProtectedLocalStorage _storage;
    private readonly AuthState _state;

    public AuthService(
        IHttpClientFactory httpFactory,
        ProtectedLocalStorage storage,
        AuthState state)
    {
        _httpFactory = httpFactory;
        _storage = storage;
        _state = state;
    }

    /// <summary>
    /// Exposes the no-auth HttpClient for use in Login.razor
    /// (register, forgot-password, reset-password are all public endpoints)
    /// </summary>
    public HttpClient GetPublicClient() => _httpFactory.CreateClient("ApiNoAuth");
    // Loads auth data from browser storage after first render
    public async Task InitializeFromStorageAsync()
    {
        try
        {
            var token = await ReadStorageAsync(TokenKey);
            var role = await ReadStorageAsync(RoleKey);
            var name = await ReadStorageAsync(NameKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                _state.SetAuth(token, role, name);
            }
            else
            {
                _state.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AUTH INIT ERROR: {ex.Message}");
            _state.Clear();
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Login must use the client without JWT handler
            var http = _httpFactory.CreateClient("ApiNoAuth");

            var resp = await http.PostAsJsonAsync("/api/v5/auth/login", new { email, password });

            if (!resp.IsSuccessStatusCode)
                return false;

            var data = await resp.Content.ReadFromJsonAsync<LoginResult>();

            if (data is null || string.IsNullOrWhiteSpace(data.Token))
                return false;

            // Save to browser storage
            await _storage.SetAsync(TokenKey, data.Token);
            await _storage.SetAsync(RoleKey, data.Role ?? "");
            await _storage.SetAsync(NameKey, data.FullName ?? "");

            // Update in-memory auth state immediately
            _state.SetAuth(data.Token, data.Role, data.FullName);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LOGIN ERROR: {ex.Message}");
            _state.Clear();
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _storage.DeleteAsync(TokenKey);
            await _storage.DeleteAsync(RoleKey);
            await _storage.DeleteAsync(NameKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LOGOUT ERROR: {ex.Message}");
        }

        _state.Clear();
    }

    private async Task<string?> ReadStorageAsync(string key)
    {
        var result = await _storage.GetAsync<string>(key);
        return result.Success ? result.Value : null;
    }

    private sealed class LoginResult
    {
        public string Token { get; set; } = "";
        public string Role { get; set; } = "";
        public string FullName { get; set; } = "";
    }
}