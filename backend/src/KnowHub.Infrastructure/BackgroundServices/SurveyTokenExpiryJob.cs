using KnowHub.Application.Contracts.Surveys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.BackgroundServices;

/// <summary>
/// Runs hourly to mark Sent invitations whose ExpiresAt has passed as Expired.
/// CRITICAL: BackgroundService is singleton — uses IServiceScopeFactory for scoped dependencies.
/// </summary>
public sealed class SurveyTokenExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SurveyTokenExpiryJob> _logger;

    public SurveyTokenExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SurveyTokenExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait first so the job doesn't run immediately on app start
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                try
                {
                    using var scope      = _scopeFactory.CreateScope();
                    var invService = scope.ServiceProvider.GetRequiredService<ISurveyInvitationService>();
                    await invService.MarkExpiredAsync(stoppingToken);
                    _logger.LogInformation("SurveyTokenExpiryJob completed at {UtcNow}", DateTime.UtcNow);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SurveyTokenExpiryJob failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SurveyTokenExpiryJob stopping (graceful shutdown).");
        }
    }
}
