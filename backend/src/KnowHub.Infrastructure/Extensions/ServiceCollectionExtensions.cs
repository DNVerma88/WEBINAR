using FluentValidation;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.AI;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Application.Contracts.Analytics;
using KnowHub.Application.Contracts.Email;
using KnowHub.Application.Contracts.Integrations;
using KnowHub.Application.Contracts.Moderation;
using KnowHub.Application.Contracts.PeerReview;
using KnowHub.Application.Contracts.SpeakerMarketplace;
using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Application.Validators;
using KnowHub.Infrastructure.AI;
using KnowHub.Infrastructure.BackgroundServices;
using KnowHub.Infrastructure.Email;
using KnowHub.Infrastructure.Integrations;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Services;
using KnowHub.Infrastructure.Services.Assessment;
using KnowHub.Infrastructure.Services.Surveys;
using KnowHub.Infrastructure.Services.Talent;
using KnowHub.Infrastructure.Storage;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KnowHub.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<KnowHubDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(KnowHubDbContext).Assembly.FullName)));

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        // TenantId provider for EF Core global query filter
        services.AddScoped<ITenantIdProvider, TenantIdProvider>();

        // Phase 1
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ISessionProposalService, SessionProposalService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ICommunityService, CommunityService>();
        services.AddScoped<IKnowledgeRequestService, KnowledgeRequestService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISpeakerService, SpeakerService>();

        // Dev Community Posts
        services.AddScoped<ICommunityPostService, CommunityPostService>();
        services.AddScoped<IPostReactionService, PostReactionService>();
        services.AddScoped<IPostCommentService, PostCommentService>();
        services.AddScoped<IPostBookmarkService, PostBookmarkService>();

        // Dev Community — Feed, User Follows, Series, Content Moderation
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IUserFollowService, UserFollowService>();
        services.AddScoped<IPostSeriesService, PostSeriesService>();
        services.AddScoped<IContentModerationService, ContentModerationService>();

        // Redis distributed cache (falls back gracefully when connection string is absent)
        var redisConn = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<IDistributedCommunityCache, RedisCommunityCache>();
        }
        else
        {
            services.AddSingleton<IDistributedCommunityCache, NoOpCommunityCache>();
        }

        // Phase 2
        services.AddScoped<IXpService, XpService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<ILearningPathService, LearningPathService>();
        services.AddScoped<ISessionQuizService, SessionQuizService>();
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<IKnowledgeAssetService, KnowledgeAssetService>();
        services.AddScoped<IKnowledgeBundleService, KnowledgeBundleService>();
        services.AddScoped<IAfterActionReviewService, AfterActionReviewService>();
        services.AddScoped<ISessionChapterService, SessionChapterService>();
        services.AddScoped<ICommunityWikiService, CommunityWikiService>();
        services.AddScoped<ISkillEndorsementService, SkillEndorsementService>();
        services.AddScoped<IMentoringService, MentoringService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILearningPathCohortService, LearningPathCohortService>();

        // Dev Community background services
        services.AddHostedService<PostSchedulerService>();
        services.AddHostedService<TrendingScorerService>();
        services.AddHostedService<CommunityDigestService>();

        // Phase 3 — Email
        services.Configure<EmailConfiguration>(configuration.GetSection("Email"));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddHostedService<WeeklyDigestBackgroundService>();

        // Phase 3 — Analytics
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Phase 3 — Moderation
        services.AddScoped<IModerationService, ModerationService>();

        // Phase 3 — Peer Review
        services.AddScoped<IPeerReviewService, PeerReviewService>();

        // Phase 3 — Speaker Marketplace
        services.AddScoped<ISpeakerMarketplaceService, SpeakerMarketplaceService>();

        // Phase 3 — AI (stub until API keys configured)
        services.Configure<AiConfiguration>(configuration.GetSection("AI"));
        services.AddScoped<IAiService, StubAiService>();

        // Phase 3 — Enterprise Integrations (stubs)
        services.Configure<IntegrationsConfiguration>(configuration.GetSection("Integrations"));
        services.AddScoped<ITeamsNotificationService, StubTeamsNotificationService>();
        services.AddScoped<ISlackNotificationService, StubSlackNotificationService>();
        services.AddScoped<ICalendarIntegrationService, StubCalendarIntegrationService>();

        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Scoped);

        // -- Survey Module ---------------------------------------------------------
        services.AddSingleton(
            System.Threading.Channels.Channel.CreateBounded<Guid>(
                new System.Threading.Channels.BoundedChannelOptions(500)
                {
                    FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait,
                    SingleReader = true,
                }));
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<ISurveyInvitationService, SurveyInvitationService>();
        services.AddScoped<ISurveyResponseService, SurveyResponseService>();
        services.AddScoped<ISurveyAnalyticsService, SurveyAnalyticsService>();
        services.AddHostedService<SurveyLaunchJob>();
        services.AddHostedService<SurveyTokenExpiryJob>();

        // Assessment Module
        services.AddScoped<IAssessmentGroupService, AssessmentGroupService>();
        services.AddScoped<IAssessmentPeriodService, AssessmentPeriodService>();
        services.AddScoped<IRatingScaleService, RatingScaleService>();
        services.AddScoped<IRubricService, RubricService>();
        services.AddScoped<IParameterService, ParameterService>();
        services.AddScoped<IEmployeeAssessmentService, EmployeeAssessmentService>();
        services.AddScoped<IAssessmentReportService, AssessmentReportService>();
        services.AddScoped<IAssessmentAuditService, AssessmentAuditService>();
        services.AddScoped<IWorkRoleService, WorkRoleService>();

        services.AddTalentModule(configuration);

        return services;
    }

    public static IServiceCollection AddTalentModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageConfiguration>(configuration.GetSection(StorageConfiguration.SectionName));
        services.Configure<TalentModuleConfiguration>(configuration.GetSection(TalentModuleConfiguration.SectionName));

        // Local storage is always available
        services.AddTransient<IStorageProvider, LocalStorageProvider>();

        // Azure Blob — register only when connection string is present
        var azureConnectionString = configuration
            .GetSection(StorageConfiguration.SectionName)
            .GetValue<string>("AzureBlob:ConnectionString");
        if (!string.IsNullOrWhiteSpace(azureConnectionString))
            services.AddTransient<IStorageProvider, AzureBlobStorageProvider>();

        // AWS S3 — always register; credentials come from environment / IAM role
        services.AddTransient<Amazon.S3.IAmazonS3>(_ =>
            new Amazon.S3.AmazonS3Client(
                Amazon.RegionEndpoint.GetBySystemName(
                    configuration.GetSection(StorageConfiguration.SectionName)
                                 .GetValue<string>("AwsS3:Region") ?? "us-east-1")));
        services.AddTransient<IStorageProvider, AwsS3StorageProvider>();

        // SharePoint / OneDrive (Graph delegated token)
        services.AddTransient<IStorageProvider, SharePointOneDriveStorageProvider>();

        services.AddSingleton<StorageProviderFactory>();

        // Resume Builder
        services.AddTransient<ResumeTextExtractor>();
        services.AddSingleton<ResumeGenerator>();
        services.AddScoped<IResumeBuilderService, ResumeBuilderService>();
        services.AddScoped<IResumeParserService, ResumeParserService>();

        // Resume Screener — named HttpClients with resilience policies (retry + circuit breaker)\n        // to protect against slow/unavailable AI providers (OWASP A10 / availability).
        services.AddHttpClient("OpenAIScoring", c =>
        {
            c.BaseAddress = new Uri("https://api.openai.com/");
            c.Timeout = TimeSpan.FromSeconds(90);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.6;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddHttpClient("OpenAIEmbeddings", c =>
        {
            c.BaseAddress = new Uri("https://api.openai.com/");
            c.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient("GeminiScoring", c =>
        {
            c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            c.Timeout = TimeSpan.FromSeconds(120);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromSeconds(3);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 3;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(150);
        });
        services.AddSingleton<IScreeningJobQueue, ScreeningJobQueue>();
        services.AddTransient<ResumeScorer>();
        services.AddScoped<IResumeScreenerService, ResumeScreenerService>();
        services.AddHostedService<BulkScreeningBackgroundService>();

        return services;
    }
}
