using KnowHub.Application.Contracts.Surveys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.BackgroundServices;

/// <summary>
/// Consumes surveyIds from the in-memory channel written by SurveyService.LaunchAsync.
/// Creates invitations and sends emails in the background so the HTTP request returns 202 immediately.
/// CRITICAL: BackgroundService is singleton — uses IServiceScopeFactory for scoped dependencies.
/// </summary>
public sealed class SurveyLaunchJob : BackgroundService
{
    private readonly System.Threading.Channels.Channel<Guid> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SurveyLaunchJob> _logger;

    public SurveyLaunchJob(
        System.Threading.Channels.Channel<Guid> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<SurveyLaunchJob> logger)
    {
        _channel      = channel;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var surveyId in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var invitationService = scope.ServiceProvider
                        .GetRequiredService<ISurveyInvitationService>();
                    await invitationService.CreateInvitationsAndSendAsync(surveyId, stoppingToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SurveyLaunchJob failed for surveyId {SurveyId}", surveyId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SurveyLaunchJob stopping (graceful shutdown).");
        }
    }
}
