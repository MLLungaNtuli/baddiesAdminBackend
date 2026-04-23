// Models/AuthModels.cs
namespace Baddies.Admin.Api.Models
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class TokenRefreshRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class MfaVerifyRequest
    {
        public string Code { get; set; } = string.Empty;
        public string MfaToken { get; set; } = string.Empty;
    }

    public class DeleteRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}