namespace DeliInventoryManagement_1.Api.ModelsV5.Auth;

/// <summary>
/// Used by POST /api/v5/auth/register
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

/// <summary>
/// Used by POST /api/v5/auth/forgot-password
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}

/// <summary>
/// Used by POST /api/v5/auth/reset-password
/// </summary>
public class ResetPasswordRequest
{
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
