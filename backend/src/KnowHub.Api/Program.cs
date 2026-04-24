using KnowHub.Api.Extensions;
using KnowHub.Api.Infrastructure;
using KnowHub.Infrastructure.Extensions;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup errors before full configuration is loaded
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // QuestPDF license — Community license is free for organisations with < $1M revenue.
    QuestPDF.Settings.License = LicenseType.Community;

    var builder = WebApplication.CreateBuilder(args);

    // Replace the default Microsoft logging with Serilog (structured JSON output in production)
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} {Properties}{NewLine}{Exception}");
    });

    builder.Services.AddControllers().AddJsonOptions(o =>
    {
        // Treat DateTime values without timezone info as UTC so Npgsql (timestamptz) never
        // receives DateTimeKind.Unspecified from a datetime-local HTML input.
        o.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        // Serialize enums as strings (e.g. "Pending" instead of 0).
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiAuthentication(builder.Configuration);
    builder.Services.AddApiCors(builder.Configuration);
    builder.Services.AddApiSignalR();
    builder.Services.AddApiRateLimiting();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Add Serilog request logging (replaces verbose Microsoft.AspNetCore.Hosting logs)
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseRequestPipeline();
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

