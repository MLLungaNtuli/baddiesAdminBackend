public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Check if headers already exist before setting
        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            context.Response.Headers["X-Frame-Options"] = "DENY";
            
        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            
        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            
        if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
        if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        
        // CSP - adjust as needed for your app
        var csp = "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: https:; " +
                  "font-src 'self'; " +
                  "connect-src 'self' https:;";
        
        if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            context.Response.Headers["Content-Security-Policy"] = csp;

        await _next(context);
    }
}