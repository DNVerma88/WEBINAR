namespace KnowHub.Api.Middleware;

/// <summary>
/// Injects OWASP-recommended security headers on every response.
/// CSP is relaxed in Development to allow Vite HMR; strict in Production.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;

        // Prevent MIME-type sniffing (OWASP A05)
        h["X-Content-Type-Options"] = "nosniff";

        // Block clickjacking (OWASP A05)
        h["X-Frame-Options"] = "DENY";

        // Limit referrer information leakage
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Disable legacy XSS auditor — rely on CSP instead (OWASP A03)
        h["X-XSS-Protection"] = "0";

        // Restrict browser feature access (OWASP A05)
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

        // Force HTTPS for one year — only on HTTPS and non-dev (OWASP A02)
        if (!_env.IsDevelopment() && context.Request.IsHttps)
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        // Content-Security-Policy (OWASP A03)
        h["Content-Security-Policy"] = _env.IsDevelopment() ? DevCsp : ProdCsp;

        // No caching of API responses containing sensitive data (OWASP A02)
        h["Cache-Control"] = "no-store";
        h["Pragma"] = "no-cache";

        await _next(context);
    }

    // Development: relaxed for Vite HMR / webpack-dev-server websockets
    private const string DevCsp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self' ws: wss: http://localhost:* https://localhost:*; " +
        "font-src 'self' data:; " +
        "frame-ancestors 'none';";

    // Production: strict — no inline scripts, upgrade insecure requests
    private const string ProdCsp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self' wss:; " +
        "font-src 'self'; " +
        "frame-ancestors 'none'; " +
        "upgrade-insecure-requests;";
}
