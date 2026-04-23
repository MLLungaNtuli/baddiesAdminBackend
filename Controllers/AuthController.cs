using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Baddies.Admin.Api.Services.Security; 
using Baddies.Admin.Api.Models;

namespace Baddies.Admin.Api.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly RateLimiterService _rateLimiter;
        private readonly ILogger<AuthController> _logger;
        private readonly DbConnectionFactory _dbFactory;

        public AuthController(
            AuthService authService,
            RateLimiterService rateLimiter,
            ILogger<AuthController> logger,
            DbConnectionFactory dbFactory)
        {
            _authService = authService;
            _rateLimiter = rateLimiter;
            _logger = logger;
            _dbFactory = dbFactory;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
        {
            // Rate limiting
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!await _rateLimiter.AllowRequestAsync(ipAddress!, "login", 5, TimeSpan.FromMinutes(15)))
            {
                return StatusCode(429, new { error = "Too many login attempts. Please try again later." });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.AuthenticateAsync(
                request.Username, 
                request.Password, 
                request.RememberMe);

            if (response == null)
                return Unauthorized(new { error = "Invalid credentials" });

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRefreshRequest request)
        {
            var response = await _authService.RefreshTokensAsync(
                request.AccessToken, 
                request.RefreshToken);

            if (response == null)
                return Unauthorized(new { error = "Invalid or expired refresh token" });

            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();
            
            var success = await _authService.LogoutAsync(adminId, request.RefreshToken);
            
            if (success)
                return Ok(new { message = "Logged out successfully" });
            
            return BadRequest(new { error = "Failed to logout" });
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();
            
            await _authService.RevokeAllSessionsAsync(adminId, "logout_all");
            
            return Ok(new { message = "All sessions logged out" });
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();
            
            using var conn = _dbFactory.Create();
            var sessions = await conn.QueryAsync<dynamic>(
                @"SELECT 
                    id,
                    device_info->>'Browser' as browser,
                    device_info->>'OS' as os,
                    device_info->>'Device' as device,
                    ip_address,
                    created_at,
                    last_used_at,
                    expires_at
                  FROM admin_sessions 
                  WHERE admin_id = @adminId 
                  AND revoked_at IS NULL
                  AND expires_at > NOW()
                  ORDER BY last_used_at DESC",
                new { adminId });
            
            return Ok(sessions);
        }

        [Authorize]
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();
            
            using var conn = _dbFactory.Create();
            var affected = await conn.ExecuteAsync(
                @"UPDATE admin_sessions 
                  SET revoked_at = NOW(), revoked_reason = 'revoked_by_user'
                  WHERE id = @sessionId 
                  AND admin_id = @adminId
                  AND revoked_at IS NULL",
                new { sessionId, adminId });
            
            if (affected > 0)
                return Ok(new { message = "Session revoked" });
            
            return NotFound(new { error = "Session not found" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var usernameClaim = User.FindFirst(ClaimTypes.Name);
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();
            
            using var conn = _dbFactory.Create();
            var admin = await conn.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT 
                    id, email, username, full_name, role, 
                    mfa_enabled, last_login, created_at
                  FROM admins 
                  WHERE id = @adminId AND deleted_at IS NULL",
                new { adminId });
            
            if (admin == null)
                return NotFound(new { error = "Admin not found" });
            
            return Ok(new
            {
                admin.id,
                admin.email,
                admin.username,
                admin.full_name,
                admin.role,
                admin.mfa_enabled,
                admin.last_login,
                admin.created_at,
                permissions = User.FindFirst("permissions")?.Value?.Split(',') ?? Array.Empty<string>()
            });
        }

        [AllowAnonymous]
        [HttpPost("debug-test")]
        public IActionResult DebugTest([FromBody] DebugTestRequest request)
        {
            _logger.LogInformation("Debug test for password: {Password}", request.Password);
            
            // Test the exact hash from your database
            var testHash = "$2a$12$a1DLNyB6OvdJWr4aBkBPsem5vKqhiUaW2rQhZfOO8H5a24Ecb4sOe";
            
            try
            {
                // Test with BCrypt directly
                var result1 = BCrypt.Net.BCrypt.Verify(request.Password, testHash);
                _logger.LogInformation("BCrypt.Verify result: {Result}", result1);
                
                var result2 = BCrypt.Net.BCrypt.EnhancedVerify(request.Password, testHash);
                _logger.LogInformation("BCrypt.EnhancedVerify result: {Result}", result2);
                
                // Test with your PasswordHasher
                var result3 = PasswordHasher.Verify(request.Password, testHash);
                _logger.LogInformation("PasswordHasher.Verify result: {Result}", result3);
                
                // Generate a new hash for comparison
                var newHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
                _logger.LogInformation("New hash: {Hash}", newHash);
                
                return Ok(new {
                    testHash,
                    bcryptVerify = result1,
                    enhancedVerify = result2,
                    passwordHasherVerify = result3,
                    newHashGenerated = newHash,
                    testHashLength = testHash.Length,
                    newHashLength = newHash.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Debug test failed");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class DebugTestRequest
    {
        public string Password { get; set; } = string.Empty;
    }
}