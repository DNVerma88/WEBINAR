using KnowHub.Application.Contracts.AI;
using KnowHub.Application.Models;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.AI;

// TODO: Replace with real OpenAI/Azure OpenAI calls once API keys are configured.
// This stub returns deterministic sample data so all dependent UIs can be developed and tested.
public class StubAiService : IAiService
{
    private readonly AiConfiguration _config;
    private readonly KnowHubDbContext _db;
    private readonly ILogger<StubAiService> _logger;

    public StubAiService(
        IOptions<AiConfiguration> config,
        KnowHubDbContext db,
        ILogger<StubAiService> logger)
    {
        _config = config.Value;
        _db = db;
        _logger = logger;
    }

    public async Task<AiSummaryResponse> GenerateSessionSummaryAsync(
        Guid sessionId, string transcriptText, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StubAiService: GenerateSessionSummaryAsync called for session {SessionId}. " +
            "Configure AI:OpenAI:ApiKey in appsettings to enable real summaries.", sessionId);

        var session = await _db.Sessions
            .Where(s => s.Id == sessionId)
            .AsNoTracking()
            .Select(s => new { s.Title })
            .FirstOrDefaultAsync(cancellationToken);

        return new AiSummaryResponse(
            sessionId,
            $"[AI summary not yet enabled] This session covers the key concepts from \"{session?.Title ?? "Unknown Session"}\". " +
            "Configure the OpenAI API key to generate real AI-powered summaries.",
            new List<string>
            {
                "Key concept 1 from the session transcript",
                "Key concept 2 from the session transcript",
                "Key concept 3 from the session transcript"
            },
            new List<string>
            {
                "Review the provided materials",
                "Complete the session quiz to test your understanding"
            },
            DateTime.UtcNow);
    }

    public async Task<List<LearningPathRecommendationDto>> GetPersonalisedRecommendationsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StubAiService: GetPersonalisedRecommendationsAsync for user {UserId}. " +
            "Configure AI:OpenAI:ApiKey to enable real recommendations.", userId);

        var paths = await _db.LearningPaths
            .Where(lp => lp.TenantId == _db.LearningPaths
                .Where(l => l.Id == lp.Id)
                .Select(l => l.TenantId)
                .FirstOrDefault() && lp.IsPublished)
            .OrderByDescending(lp => lp.CreatedDate)
            .Take(3)
            .AsNoTracking()
            .Select(lp => new LearningPathRecommendationDto(
                lp.Id,
                lp.Title,
                lp.Description ?? "No description available",
                lp.DifficultyLevel.ToString(),
                "This path aligns with your current skill level and learning history.",
                0.85))
            .ToListAsync(cancellationToken);

        return paths;
    }

    public async Task<List<KnowledgeGapDto>> DetectKnowledgeGapsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StubAiService: DetectKnowledgeGapsAsync for user {UserId}. " +
            "Configure AI:OpenAI:ApiKey to enable real gap detection.", userId);

        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Take(3)
            .AsNoTracking()
            .Select(c => new KnowledgeGapDto(
                c.Id,
                c.Name,
                $"Limited quiz participation detected in {c.Name}. Attending sessions and completing quizzes in this area will improve your score.",
                5,
                60.0,
                new List<string> { "fundamentals", "best-practices" },
                new List<string>
                {
                    $"Enroll in a {c.Name} learning path",
                    $"Attend upcoming {c.Name} sessions",
                    "Complete knowledge quizzes"
                }))
            .ToListAsync(cancellationToken);

        return categories;
    }

    public async Task<PagedResult<TranscriptSearchResultDto>> SearchTranscriptAsync(
        TranscriptSearchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StubAiService: SearchTranscriptAsync for term '{Term}'. " +
            "Configure AI:OpenAI:ApiKey to enable real semantic search.", request.SearchTerm);

        var sessions = await _db.Sessions
            .Where(s => s.RecordingUrl != null &&
                (s.Title.ToLower().Contains(request.SearchTerm.ToLower()) ||
                 (s.Description != null && s.Description.ToLower().Contains(request.SearchTerm.ToLower()))))
            .OrderByDescending(s => s.ScheduledAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .Join(_db.Users, s => s.SpeakerId, u => u.Id, (s, u) => new TranscriptSearchResultDto(
                s.Id, s.Title, u.FullName, s.ScheduledAt,
                $"...matched content related to \"{request.SearchTerm}\" found in this session...",
                0.75))
            .ToListAsync(cancellationToken);

        var total = await _db.Sessions
            .CountAsync(s => s.RecordingUrl != null &&
                (s.Title.ToLower().Contains(request.SearchTerm.ToLower()) ||
                 (s.Description != null && s.Description.ToLower().Contains(request.SearchTerm.ToLower()))),
                cancellationToken);

        return new PagedResult<TranscriptSearchResultDto> { Data = sessions, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }
}
