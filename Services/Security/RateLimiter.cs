// Services/Security/RateLimiter.cs
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public class RateLimiter
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(
        DbConnectionFactory dbFactory,
        IMemoryCache cache,
        ILogger<RateLimiter> logger)
    {
        _dbFactory = dbFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> AllowRequest(string identifier, string endpoint, int maxRequests, TimeSpan timeWindow)
    {
        if (string.IsNullOrEmpty(identifier))
            return true;

        // Check if IP is banned
        if (await IsIpBannedAsync(identifier))
        {
            _logger.LogWarning("Banned IP attempted access: {IP}", identifier);
            return false;
        }

        var cacheKey = $"ratelimit:{identifier}:{endpoint}";
        
        if (_cache.TryGetValue(cacheKey, out RateLimitInfo info))
        {
            if (info.Count >= maxRequests)
            {
                if (DateTime.UtcNow - info.FirstRequest < timeWindow)
                {
                    await LogRateLimitHitAsync(identifier, endpoint, maxRequests);
                    return false;
                }
                else
                {
                    // Reset window
                    info = new RateLimitInfo { Count = 1, FirstRequest = DateTime.UtcNow };
                }
            }
            else
            {
                info.Count++;
            }
        }
        else
        {
            info = new RateLimitInfo { Count = 1, FirstRequest = DateTime.UtcNow };
        }

        _cache.Set(cacheKey, info, timeWindow);
        return true;
    }

    private async Task<bool> IsIpBannedAsync(string ipAddress)
    {
        try
        {
            using var conn = _dbFactory.Create();
            return await conn.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM banned_ips 
                    WHERE ip_address = @ipAddress::inet 
                    AND (expires_at IS NULL OR expires_at > NOW())
                )",
                new { ipAddress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking banned IPs");
            return false;
        }
    }

    private async Task LogRateLimitHitAsync(string identifier, string endpoint, int maxRequests)
    {
        try
        {
            using var conn = _dbFactory.Create();
            await conn.ExecuteAsync(
                @"INSERT INTO security_events 
                  (event_type, ip_address, details)
                  VALUES 
                  ('rate_limit_exceeded', @ipAddress, @details)",
                new
                {
                    ipAddress = identifier,
                    details = JsonSerializer.Serialize(new
                    {
                        endpoint,
                        maxRequests,
                        timestamp = DateTime.UtcNow
                    })
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log rate limit hit");
        }
    }
}