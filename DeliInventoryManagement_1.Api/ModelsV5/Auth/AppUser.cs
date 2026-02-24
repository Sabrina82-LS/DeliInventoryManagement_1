namespace DeliInventoryManagement_1.Api.ModelsV5.Auth;

public class AppUser
{
    public string id { get; set; } = "";
    public string pk { get; set; } = "USER";

    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Staff";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}