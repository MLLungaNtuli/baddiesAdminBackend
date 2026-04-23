// Middleware/AuditMiddleware.cs
using System.Diagnostics;
using System.Net;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, DbConnectionFactory dbFactory)
    {
        var stopwatch = Stopwatch.StartNew();
        var adminIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
        
        // Get IP address and handle null cases
        var ipAddress = context.Connection.RemoteIpAddress;
        string ipAddressString = ipAddress?.ToString() ?? "0.0.0.0";
        
        // For IPv6 loopback, convert to IPv4 for better compatibility
        if (ipAddressString == "::1")
            ipAddressString = "127.0.0.1";
            
        string userAgent = context.Request.Headers.UserAgent.ToString() ?? "unknown";
        
        try
        {
            await _next(context);
            stopwatch.Stop();
            
            // Log successful request
            if (adminIdClaim != null && Guid.TryParse(adminIdClaim.Value, out var adminId))
            {
                using var conn = dbFactory.Create();
                await conn.ExecuteAsync(
                    @"INSERT INTO admin_activity_logs 
                      (admin_id, action, resource_type, details, ip_address, user_agent)
                      VALUES 
                      (@adminId, @action, @resourceType, @details::jsonb, CAST(@ipAddress AS inet), @userAgent)",
                    new
                    {
                        adminId,
                        action = $"{context.Request.Method}_{context.Request.Path}",
                        resourceType = context.Request.Path.Value?.Split('/').Length > 2
                            ? context.Request.Path.Value?.Split('/')[2]
                            : null,
                        details = JsonSerializer.Serialize(new
                        {
                            method = context.Request.Method,
                            path = context.Request.Path,
                            statusCode = context.Response.StatusCode,
                            durationMs = stopwatch.ElapsedMilliseconds,
                            query = context.Request.QueryString.Value
                        }),
                        ipAddress = ipAddressString, // Now explicitly cast to inet
                        userAgent = userAgent
                    });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Log failed request
            _logger.LogError(ex, "Request failed: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            
            if (adminIdClaim != null && Guid.TryParse(adminIdClaim.Value, out var adminId))
            {
                using var conn = dbFactory.Create();
                try
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO admin_activity_logs 
                          (admin_id, action, resource_type, details, ip_address, user_agent)
                          VALUES 
                          (@adminId, @action, @resourceType, @details::jsonb, CAST(@ipAddress AS inet), @userAgent)",
                        new
                        {
                            adminId,
                            action = $"{context.Request.Method}_{context.Request.Path}_ERROR",
                            resourceType = context.Request.Path.Value?.Split('/').Length > 2 
                                ? context.Request.Path.Value?.Split('/')[2] 
                                : null,
                            details = JsonSerializer.Serialize(new
                            {
                                method = context.Request.Method,
                                path = context.Request.Path,
                                error = ex.Message,
                                durationMs = stopwatch.ElapsedMilliseconds
                            }),
                            ipAddress = ipAddressString,
                            userAgent = userAgent
                        });
                }
                catch (Exception logEx)
                {
                    // Don't let logging failure break the request
                    _logger.LogError(logEx, "Failed to log to admin_activity_logs");
                }
            }
            
            throw;
        }
    }
}