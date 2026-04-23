using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using System.Text.Json;
using Baddies.Admin.Api.Models;

public class EnhancedJwtService
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _securityKey;
    private readonly ILogger<EnhancedJwtService> _logger;

    public EnhancedJwtService(
        DbConnectionFactory dbFactory,
        IConfiguration config,
        ILogger<EnhancedJwtService> logger)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
        
        var key = _config["Jwt:Key"] ?? throw new Exception("JWT Key is missing");
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public async Task<AuthResponse> GenerateTokensAsync(
        Admin admin, 
        HttpContext httpContext, 
        bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Username),
            new(ClaimTypes.Role, admin.Role),
            new("admin_id", admin.Id.ToString()),
            new("username", admin.Username)
        };

        // Get permissions for admin
        var permissions = await GetAdminPermissionsAsync(admin.Id);
        if (permissions.Any())
        {
            claims.Add(new Claim("permissions", string.Join(",", permissions)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        // Generate refresh token
        var refreshToken = GenerateSecureRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        // Store refresh token in database
        await StoreRefreshTokenAsync(admin.Id, refreshTokenHash, rememberMe, httpContext);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900, // 15 minutes
            TokenType = "Bearer"
        };
    }

    public async Task<AuthResponse> RefreshTokensAsync(
        string accessToken, 
        string refreshToken, 
        HttpContext httpContext)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            var adminId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var refreshTokenHash = HashToken(refreshToken);

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
                _logger.LogWarning("Refresh token not found or invalid for admin {AdminId}", adminId);
                return null!;
            }

            // Update last used time
            await conn.ExecuteAsync(
                @"UPDATE admin_sessions SET last_used_at = NOW() WHERE id = @sessionId",
                new { sessionId = session.Id });

            // Get admin info
            var admin = await conn.QuerySingleOrDefaultAsync<Admin>(
                "SELECT * FROM admins WHERE id = @id AND deleted_at IS NULL",
                new { id = adminId });

            if (admin == null)
                return null!;

            // Generate new tokens
            return await GenerateTokensAsync(admin, httpContext, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return null!;
        }
    }

    public async Task RevokeRefreshTokenAsync(Guid adminId, string refreshToken, string reason)
    {
        var refreshTokenHash = HashToken(refreshToken);
        
        using var conn = _dbFactory.Create();
        await conn.ExecuteAsync(
            @"UPDATE admin_sessions 
              SET revoked_at = NOW(), revoked_reason = @reason
              WHERE admin_id = @adminId 
              AND refresh_token_hash = @refreshTokenHash
              AND revoked_at IS NULL",
            new { adminId, refreshTokenHash, reason });
    }

    public async Task RevokeAllSessionsAsync(Guid adminId, string reason)
    {
        using var conn = _dbFactory.Create();
        await conn.ExecuteAsync(
            @"UPDATE admin_sessions 
              SET revoked_at = NOW(), revoked_reason = @reason
              WHERE admin_id = @adminId 
              AND revoked_at IS NULL",
            new { adminId, reason });
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

    private async Task StoreRefreshTokenAsync(
        Guid adminId, 
        string refreshTokenHash, 
        bool rememberMe, 
        HttpContext httpContext)
    {
        var deviceInfo = ExtractDeviceInfo(httpContext);
        var expiryDays = rememberMe ? 30 : 7;

        using var conn = _dbFactory.Create();
        await conn.ExecuteAsync(
            @"INSERT INTO admin_sessions 
              (admin_id, refresh_token_hash, device_info, ip_address, user_agent, expires_at, created_at, last_used_at)
              VALUES 
              (@adminId, @refreshTokenHash, @deviceInfo, @ipAddress, @userAgent, NOW() + INTERVAL '@expiryDays days', NOW(), NOW())",
            new
            {
                adminId,
                refreshTokenHash,
                deviceInfo = JsonSerializer.Serialize(deviceInfo),
                ipAddress = deviceInfo.IpAddress,
                userAgent = deviceInfo.UserAgent,
                expiryDays
            });
    }

    private DeviceInfo ExtractDeviceInfo(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return new DeviceInfo
        {
            UserAgent = userAgent,
            Browser = GetBrowserFromUserAgent(userAgent),
            OS = GetOSFromUserAgent(userAgent),
            Device = "Unknown",
            IpAddress = ipAddress,
            DeviceFingerprint = GenerateDeviceFingerprint(context),
            LastSeen = DateTime.UtcNow
        };
    }

    private string GetBrowserFromUserAgent(string userAgent)
    {
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        return "Unknown";
    }

    private string GetOSFromUserAgent(string userAgent)
    {
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac")) return "macOS";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iOS")) return "iOS";
        return "Unknown";
    }

    private string GenerateDeviceFingerprint(HttpContext context)
    {
        var components = new List<string?>
        {
            context.Request.Headers["User-Agent"].ToString(),
            context.Request.Headers["Accept-Language"].ToString(),
            context.Connection.RemoteIpAddress?.ToString()
        };
        
        var fingerprint = string.Join("|", components.Where(c => !string.IsNullOrEmpty(c)));
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(hash).ToLower();
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
    }

    public static string GenerateSecureRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}