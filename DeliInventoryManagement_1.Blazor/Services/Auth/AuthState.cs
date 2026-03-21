namespace DeliInventoryManagement_1.Blazor.Services.Auth;

public sealed class AuthState
{
    public string? Token { get; private set; }
    public string? Role { get; private set; }
    public string? Name { get; private set; }

    // IMPORTANT: set true after we try to load from storage at least once
    public bool IsReady { get; private set; }

    public bool IsAuthenticated => IsReady && !string.IsNullOrWhiteSpace(Token);

    public event Action? OnChange;

    public void SetReady()
    {
        IsReady = true;
        OnChange?.Invoke();
    }

    public void SetAuth(string? token, string? role, string? name)
    {
        Token = token;
        Role = role;
        Name = name;
        IsReady = true;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        Role = null;
        Name = null;
        IsReady = true;
        OnChange?.Invoke();
    }
}