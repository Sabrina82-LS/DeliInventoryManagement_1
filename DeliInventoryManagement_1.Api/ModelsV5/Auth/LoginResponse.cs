namespace DeliInventoryManagement_1.Api.ModelsV5.Auth;

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Role { get; set; } = "";
    public string FullName { get; set; } = "";
}