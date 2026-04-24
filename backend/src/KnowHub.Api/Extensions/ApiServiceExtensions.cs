using KnowHub.Api.Hubs;
using KnowHub.Api.Infrastructure;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Talent;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace KnowHub.Api.Extensions;

public static class ApiServiceExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication.
    /// Audience validation is enabled; token is also accepted via query-string
    /// "access_token" so SignalR WebSocket upgrades can carry the JWT.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var jwtAudience = configuration["Jwt:Audience"] ?? "KnowHubClient";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Prevent ASP.NET Core from remapping claim names (e.g. "role" → ClaimTypes.Role URI)
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                // Allow JWT via query string for SignalR WebSocket connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrEmpty(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            // Role is stored as an integer bit-flag in the JWT "role" claim.
            // These policies use bitwise AND to support multi-role users.
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAssertion(ctx => HasRoleFlag(ctx, 16) || HasRoleFlag(ctx, 32)));

            options.AddPolicy("ManagerOrAbove", policy =>
                policy.RequireAssertion(ctx =>
                    HasRoleFlag(ctx, 4) || HasRoleFlag(ctx, 8) || HasRoleFlag(ctx, 16) || HasRoleFlag(ctx, 32)));

            options.AddPolicy("KnowledgeTeamOrAbove", policy =>
                policy.RequireAssertion(ctx =>
                    HasRoleFlag(ctx, 8) || HasRoleFlag(ctx, 16) || HasRoleFlag(ctx, 32)));

            options.AddPolicy("ContributorOrAbove", policy =>
                policy.RequireAssertion(ctx =>
                    HasRoleFlag(ctx, 2) || HasRoleFlag(ctx, 4) || HasRoleFlag(ctx, 8) || HasRoleFlag(ctx, 16) || HasRoleFlag(ctx, 32)));

            options.AddPolicy("AdminOrAbove", policy =>
                policy.RequireAssertion(ctx =>
                    HasRoleFlag(ctx, 16) || HasRoleFlag(ctx, 32)));
        });
        return services;
    }

    private static bool HasRoleFlag(AuthorizationHandlerContext ctx, int flag)
    {
        var claim = ctx.User.FindFirst("role")?.Value;
        return int.TryParse(claim, out var val) && (val & flag) == flag;
    }

    /// <summary>
    /// Registers CORS. Origin is read from configuration so Dev and Prod can differ.
    /// CORS is registered for all environments; the origin is environment-specific.
    /// </summary>
    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var frontendOrigin = configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";

        services.AddCors(options =>
            options.AddPolicy("FrontendPolicy", policy =>
            {
                if (frontendOrigin == "*")
                    policy.SetIsOriginAllowed(_ => true);
                else
                    policy.WithOrigins(frontendOrigin);

                policy.WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization, "X-Requested-With")
                      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                      .AllowCredentials();
            }));

        return services;
    }

    /// <summary>
    /// Registers SignalR and the <see cref="INotificationPusher"/> implementation
    /// that delivers real-time pushes to connected clients.
    /// </summary>
    public static IServiceCollection AddApiSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<INotificationPusher, SignalRNotificationPusher>();
        services.AddSingleton<IScreeningProgressPusher, SignalRScreeningProgressPusher>();
        return services;
    }

    /// <summary>
    /// Adds a global fixed-window rate limiter to help mitigate brute-force and
    /// denial-of-service attacks (OWASP A07 / A10).
    /// Each authenticated user or anonymous IP is allowed 300 requests per minute.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.User.Identity?.Name
                                  ?? ctx.Connection.RemoteIpAddress?.ToString()
                                  ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20,
                    }));

            // B6 fix: strict per-IP rate limit for auth endpoints (login / register / refresh)
            options.AddSlidingWindowLimiter("AuthPolicy", opts =>
            {
                opts.PermitLimit = 10;
                opts.Window = TimeSpan.FromMinutes(1);
                opts.SegmentsPerWindow = 6;
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = 0;
            });

            // Survey form — 10 req/min per IP (token enumeration mitigation)
            options.AddFixedWindowLimiter("SurveyTokenPolicy", o =>
            {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });

            // Survey resend — 20 req/min per IP (bulk email abuse mitigation)
            options.AddFixedWindowLimiter("AdminResendPolicy", o =>
            {
                o.PermitLimit = 20;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });

            // AI endpoints — strict limit to prevent LLM cost/quota abuse (10 req/min per user)
            options.AddSlidingWindowLimiter("AiPolicy", opts =>
            {
                opts.PermitLimit = 10;
                opts.Window = TimeSpan.FromMinutes(1);
                opts.SegmentsPerWindow = 6;
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
