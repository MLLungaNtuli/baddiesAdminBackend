using System.ComponentModel.DataAnnotations;

namespace Baddies.Admin.Api.Controllers
{
public class AdminLoginRequest
{
    [Required, EmailAddress]
    public string Username { get; set; } = string.Empty;
    
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; }
}

public class TokenRefreshRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class MfaVerifyRequest
{
    [Required, Length(6, 6)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public string MfaToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class DeleteRequest
{
    [Required, MinLength(5)]
    public string Reason { get; set; } = string.Empty;
}
}