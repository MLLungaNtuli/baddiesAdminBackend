using Dapper;
using UAParser;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using Baddies.Admin.Api.Models;

namespace Baddies.Admin.Api.Services.Security
{
    public class AuthService
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            DbConnectionFactory dbFactory,
            JwtService jwtService,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbFactory = dbFactory;
            _jwtService = jwtService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResponse?> AuthenticateAsync(string username, string password, bool rememberMe)
        {
            // FIX: Use logger directly without any extra syntax
            _logger.LogInformation("=== LOGIN ATTEMPT START ===");
            _logger.LogInformation("Username: {Username}", username);
            _logger.LogInformation("Password length: {Length}", password.Length);
            
            using var conn = _dbFactory.Create();
            
            // Get admin
            var admin = await conn.QuerySingleOrDefaultAsync<Admin>(
                @"SELECT * FROM admins 
                  WHERE (email = @username OR username = @username) 
                  AND deleted_at IS NULL",
                new { username });

            if (admin == null)
            {
                _logger.LogWarning("❌ Admin not found: {Username}", username);
                return null;
            }

            _logger.LogInformation("✅ Admin found:");
            _logger.LogInformation("   ID: {Id}", admin.Id);
            _logger.LogInformation("   Email: {Email}", admin.Email);
            _logger.LogInformation("   Username: {Username}", admin.Username);
            _logger.LogInformation("   Password hash: {Hash}", admin.PasswordHash);
            _logger.LogInformation("   Hash length: {Length}", admin.PasswordHash?.Length ?? 0);

            // Check the actual bytes of the password
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            _logger.LogInformation("Password bytes (hex): {Bytes}", BitConverter.ToString(passwordBytes));

            // Test with BCrypt directly
            _logger.LogInformation("Testing password verification...");
            
            bool passwordValid = false;
            string errorMessage = "";
            
            try
            {
                // Method 1: Direct BCrypt
                _logger.LogInformation("Method 1: BCrypt.Net.BCrypt.Verify");
                passwordValid = BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);
                _logger.LogInformation("Result: {Result}", passwordValid);
                
                if (!passwordValid)
                {
                    // Method 2: Enhanced Verify
                    _logger.LogInformation("Method 2: BCrypt.Net.BCrypt.EnhancedVerify");
                    passwordValid = BCrypt.Net.BCrypt.EnhancedVerify(password, admin.PasswordHash);
                    _logger.LogInformation("Result: {Result}", passwordValid);
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex, "BCrypt verification exception");
            }

            if (!passwordValid)
            {
                _logger.LogWarning("❌ Password verification failed");
                _logger.LogWarning("Error: {Error}", errorMessage);
                
                // Try to see what's wrong with the hash
                _logger.LogInformation("Analyzing hash structure...");
                if (!string.IsNullOrEmpty(admin.PasswordHash))
                {
                    _logger.LogInformation("Hash prefix: {Prefix}", admin.PasswordHash.Substring(0, Math.Min(30, admin.PasswordHash.Length)));
                    _logger.LogInformation("Hash expected format: $2a$12$...");
                    
                    // Check if hash looks corrupted
                    if (!admin.PasswordHash.StartsWith("$2a$"))
                    {
                        _logger.LogError("❌ Hash doesn't start with $2a$ - might be corrupted!");
                    }
                }
                
                return null;
            }

            _logger.LogInformation("✅ Password verified successfully!");
            
            // Rest of your authentication logic...
            await ResetFailedAttemptsAsync(admin.Id);
            
            // Get role
            var role = await GetAdminRoleAsync(admin.Id);
            
            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(admin.Id, admin.Username, role, new List<string>());
            var refreshToken = JwtService.GenerateSecureRefreshToken();
            var refreshTokenHash = JwtService.HashToken(refreshToken);

            // Store refresh token
            await StoreRefreshTokenAsync(admin.Id, refreshTokenHash, rememberMe);

            // Update last login
            await conn.ExecuteAsync(
                "UPDATE admins SET last_login = NOW() WHERE id = @id",
                new { id = admin.Id });

            _logger.LogInformation("✅ Login successful!");
            _logger.LogInformation("=== LOGIN ATTEMPT END ===");

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900,
                TokenType = "Bearer"
            };
        }

        private async Task<string> GetAdminRoleAsync(Guid adminId)
        {
            using var conn = _dbFactory.Create();
            var role = await conn.QuerySingleOrDefaultAsync<string>(
                @"SELECT r.name 
                  FROM admin_role_assignments ara
                  JOIN admin_roles r ON ara.role_id = r.id
                  WHERE ara.admin_id = @adminId
                  LIMIT 1",
                new { adminId });
            
            return role ?? "admin";
        }

        public async Task<AuthResponse?> RefreshTokensAsync(string accessToken, string refreshToken)
        {
            try
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
                var adminId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));
                var refreshTokenHash = JwtService.HashToken(refreshToken);

                // Validate refresh token
                using var conn = _dbFactory.Create();
                var session = await conn.QuerySingleOrDefaultAsync<AdminSession>(
                    @"SELECT * FROM admin_sessions 
                      WHERE admin_id = @adminId 
                      AND refresh_token_hash = @refreshTokenHash
                      AND revoked_at IS NULL
                      AND expires_at > NOW()",
                    new { adminId, refreshTokenHash });

                if (session == null)
                {
                    await LogSecurityEventAsync(adminId, "refresh_token_invalid", new { reason = "token_not_found" });
                    return null;
                }

                // Check for suspicious activity
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var currentDeviceFingerprint = GenerateDeviceFingerprint(httpContext);
                    var deviceInfo = JsonSerializer.Deserialize<DeviceInfo>(session.DeviceInfo);
                    
                    if (deviceInfo?.DeviceFingerprint != currentDeviceFingerprint)
                    {
                        await LogSecurityEventAsync(adminId, "suspicious_device_refresh", 
                            new { expected = deviceInfo?.DeviceFingerprint, actual = currentDeviceFingerprint });
                        await RevokeAllSessionsAsync(adminId, "suspicious_device_detected");
                        return null;
                    }
                }

                // Get admin info
                var admin = await conn.QuerySingleOrDefaultAsync<Admin>(
                    "SELECT * FROM admins WHERE id = @id AND deleted_at IS NULL",
                    new { id = adminId });

                if (admin == null)
                    return null;

                // Generate new tokens
                var permissions = await GetAdminPermissionsAsync(adminId);
                var newAccessToken = _jwtService.GenerateAccessToken(admin.Id, admin.Username, admin.Role, permissions);
                var newRefreshToken = JwtService.GenerateSecureRefreshToken();
                var newRefreshTokenHash = JwtService.HashToken(newRefreshToken);

                // Rotate refresh token
                await conn.ExecuteAsync(
                    @"UPDATE admin_sessions 
                      SET refresh_token_hash = @newRefreshTokenHash,
                          last_used_at = NOW()
                      WHERE id = @sessionId",
                    new { sessionId = session.Id, newRefreshTokenHash });

                await LogSecurityEventAsync(adminId, "token_refreshed", new { sessionId = session.Id });

                return new AuthResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 900,
                    TokenType = "Bearer"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return null;
            }
        }

        public async Task<bool> LogoutAsync(Guid adminId, string refreshToken)
        {
            using var conn = _dbFactory.Create();
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshTokenHash = JwtService.HashToken(refreshToken);
                var affected = await conn.ExecuteAsync(
                    @"UPDATE admin_sessions 
                      SET revoked_at = NOW(), revoked_reason = 'logout'
                      WHERE admin_id = @adminId 
                      AND refresh_token_hash = @refreshTokenHash
                      AND revoked_at IS NULL",
                    new { adminId, refreshTokenHash });
                
                if (affected > 0)
                {
                    await LogSecurityEventAsync(adminId, "logout", null);
                    return true;
                }
            }
            
            return false;
        }

        public async Task RevokeAllSessionsAsync(Guid adminId, string reason = "security_breach")
        {
            using var conn = _dbFactory.Create();
            await conn.ExecuteAsync(
                @"UPDATE admin_sessions 
                  SET revoked_at = NOW(), revoked_reason = @reason
                  WHERE admin_id = @adminId 
                  AND revoked_at IS NULL",
                new { adminId, reason });
            
            await LogSecurityEventAsync(adminId, "all_sessions_revoked", new { reason });
        }

        private async Task<List<string>> GetAdminPermissionsAsync(Guid adminId)
        {
            using var conn = _dbFactory.Create();
            var permissions = await conn.QueryAsync<string>(
                @"SELECT DISTINCT CONCAT(rp.resource, ':', rp.action) as permission
                  FROM admin_role_assignments ara
                  JOIN role_permissions rp ON ara.role_id = rp.role_id
                  WHERE ara.admin_id = @adminId
                  UNION
                  SELECT DISTINCT CONCAT(resource, ':', action) as permission
                  FROM admin_permissions 
                  WHERE admin_id = @adminId",
                new { adminId });
            
            return permissions.ToList();
        }

        private async Task StoreRefreshTokenAsync(Guid adminId, string refreshTokenHash, bool rememberMe)
{
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext == null) return;

    var deviceInfo = await ExtractDeviceInfoAsync(httpContext);
    var expiryDays = rememberMe ? 30 : 7;

    using var conn = _dbFactory.Create();
    await conn.ExecuteAsync(
        @"INSERT INTO admin_sessions 
          (
            admin_id,
            refresh_token_hash,
            device_info,
            ip_address,
            user_agent,
            expires_at,
            created_at,
            last_used_at
          )
          VALUES 
          (
            @adminId,
            @refreshTokenHash,
            @deviceInfo::jsonb,
            @ipAddress::inet,
            @userAgent,
            NOW() + make_interval(days => @expiryDays),
            NOW(),
            NOW()
          )",
        new
        {
            adminId,
            refreshTokenHash,
            deviceInfo = JsonSerializer.Serialize(deviceInfo),
            ipAddress = deviceInfo.IpAddress ?? "0.0.0.0",
            userAgent = deviceInfo.UserAgent,
            expiryDays
        });
}


        private async Task<DeviceInfo> ExtractDeviceInfoAsync(HttpContext context)
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);
            
            var deviceFingerprint = GenerateDeviceFingerprint(context);
            
            return new DeviceInfo
            {
                UserAgent = userAgent,
                Browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}",
                OS = $"{clientInfo.OS.Family} {clientInfo.OS.Major}",
                Device = clientInfo.Device.Family,
                IpAddress = ipAddress,
                DeviceFingerprint = deviceFingerprint,
                LastSeen = DateTime.UtcNow
            };
        }

        private string GenerateDeviceFingerprint(HttpContext context)
        {
            var components = new List<string?>
            {
                context.Request.Headers.UserAgent.ToString(),
                context.Request.Headers.AcceptLanguage.ToString(),
                
            };
            
            var fingerprint = string.Join("|", components.Where(c => !string.IsNullOrEmpty(c)));
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
            return Convert.ToHexString(hash).ToLower();
        }

        private async Task ResetFailedAttemptsAsync(Guid adminId)
        {
            using var conn = _dbFactory.Create();
            await conn.ExecuteAsync(
                @"UPDATE admins 
                  SET failed_login_attempts = 0,
                      locked_until = NULL
                  WHERE id = @adminId",
                new { adminId });
        }

        private async Task IncrementFailedAttemptsAsync(Guid adminId)
        {
            using var conn = _dbFactory.Create();
            await conn.ExecuteAsync(
                @"UPDATE admins 
                  SET failed_login_attempts = COALESCE(failed_login_attempts, 0) + 1
                  WHERE id = @adminId",
                new { adminId });
            
            // Check if we need to lock the account
            var admin = await conn.QuerySingleOrDefaultAsync<Admin>(
                "SELECT failed_login_attempts FROM admins WHERE id = @adminId",
                new { adminId });
            
            if (admin?.FailedLoginAttempts >= 5)
            {
                await conn.ExecuteAsync(
                    @"UPDATE admins 
                      SET locked_until = NOW() + INTERVAL '15 minutes'
                      WHERE id = @adminId",
                    new { adminId });
                
                await LogSecurityEventAsync(adminId, "account_locked", 
                    new { reason = "too_many_failed_attempts", duration = "15 minutes" });
            }
        }

        private async Task LogSecurityEventAsync(Guid? adminId, string eventType, object? details)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                using var conn = _dbFactory.Create();
                
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                
                // For PostgreSQL inet type, we need to handle null/empty IPs
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "0.0.0.0";
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO security_events 
                      (admin_id, event_type, ip_address, user_agent, device_fingerprint, details, created_at)
                      VALUES 
                      (@adminId, @eventType, @ipAddress::inet, @userAgent, @deviceFingerprint, @details, NOW())",
                    new
                    {
                        adminId,
                        eventType,
                        ipAddress,
                        userAgent = httpContext.Request.Headers.UserAgent.ToString() ?? "",
                        deviceFingerprint = GenerateDeviceFingerprint(httpContext),
                        details = details != null ? JsonSerializer.Serialize(details) : null
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event");
            }
        }
    
    

    // Helper classes
    public class Admin
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "admin";
        public bool MfaEnabled { get; set; }
        public string? MfaSecret { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class AdminSession
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public string RefreshTokenHash { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public DateTime LastUsedAt { get; set; }
    }

    public class DeviceInfo
    {
        public string UserAgent { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string OS { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceFingerprint { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
    }
    }

}