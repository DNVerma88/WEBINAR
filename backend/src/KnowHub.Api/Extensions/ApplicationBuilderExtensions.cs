using KnowHub.Api.Hubs;
using KnowHub.Api.Middleware;

namespace KnowHub.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Wires up the full request pipeline in the correct middleware order.
    /// </summary>
    public static WebApplication UseRequestPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSecurityHeaders();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseCors("FrontendPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");

        // Health endpoint restricted to localhost / private network; not exposed publicly.
        // In production, the load balancer's health probe originates from an internal IP.
        app.MapHealthChecks("/health").RequireHost("localhost", "127.0.0.1", "api");

        return app;
    }

    /// <summary>
    /// Inserts <see cref="SecurityHeadersMiddleware"/> into the pipeline.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
