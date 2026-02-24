namespace DeliInventoryManagement_1.Api.ModelsV5.Auth;

public class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Staff"; // Admin | Staff
}