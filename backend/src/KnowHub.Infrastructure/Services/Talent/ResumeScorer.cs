using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.Services.Talent;

public class ScoringResult
{
    public decimal SemanticSimilarityScore { get; set; }
    public decimal SkillsDepthScore { get; set; }
    public decimal LegitimacyScore { get; set; }
    public decimal OverallScore { get; set; }
    public string Recommendation { get; set; } = "NoFit";
    public string? CandidateName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ScoreSummary { get; set; }
    public List<string> SkillsMatched { get; set; } = [];
    public List<string> SkillsGap { get; set; } = [];
    public List<string> RedFlags { get; set; } = [];
}

public class ResumeScorer
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly ILogger<ResumeScorer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // Scoring weights — must sum to 1.0 when applied to their respective 0-1 / 0-100 normalised scores
    private const decimal SemanticWeight    = 35m;   // semantic similarity (0-1) × 35  → contribution up to ~35
    private const decimal SkillsDepthWeight = 0.45m; // skills depth (0-100) × 0.45    → contribution up to ~45
    private const decimal LegitimacyWeight  = 0.20m; // legitimacy (0-100) × 0.20      → contribution up to ~20

    public ResumeScorer(ILogger<ResumeScorer> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // -- Public API -------------------------------------------------------------

    /// <summary>
    /// Computes an OpenAI text-embedding-3-small vector for the given text.
    /// Returns an empty array when the call fails.
    /// </summary>
    public async Task<float[]> ComputeEmbeddingAsync(string text, string apiKey, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("OpenAIEmbeddings");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = JsonSerializer.Serialize(new { input = text, model = "text-embedding-3-small" });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("v1/embeddings", content, ct);
        if (!response.IsSuccessStatusCode) return [];

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }

    /// <summary>
    /// Scores a resume against a job description.
    /// scoringMode: "AI" = OpenAI GPT + embeddings, "Gemini" = Google Gemini, "Stub" = keyword-overlap heuristic.
    /// </summary>
    public async Task<ScoringResult> ScoreAsync(
        string jdText,
        float[] jdEmbedding,
        string resumeText,
        AiConfiguration config,
        CancellationToken ct,
        string scoringMode = ScoringModes.AI,
        string? promptTemplate = null)
    {
        if (string.Equals(scoringMode, ScoringModes.Stub, StringComparison.OrdinalIgnoreCase))
            return StubScore(jdText, resumeText);

        ILlmProvider provider = string.Equals(scoringMode, ScoringModes.Gemini, StringComparison.OrdinalIgnoreCase)
            ? new GeminiProvider(config.Gemini, _httpClientFactory)
            : new OpenAiProvider(config.OpenAI, _httpClientFactory);

        if (!provider.IsConfigured)
        {
            _logger.LogWarning("{Provider} API key is not configured — using stub scoring.", provider.Name);
            return StubScore(jdText, resumeText);
        }

        try
        {
            var prompt = BuildPrompt(jdText, resumeText, promptTemplate);
            var data   = await provider.GetScoringDataAsync(prompt, ct);

            // For OpenAI, override the LLM semantic score with the more precise embedding cosine similarity
            var semanticScore = data.SemanticSimilarityScore;
            if (!string.Equals(scoringMode, ScoringModes.Gemini, StringComparison.OrdinalIgnoreCase) && jdEmbedding.Length > 0)
            {
                var resumeEmbedding = await ComputeEmbeddingAsync(resumeText, config.OpenAI.ApiKey, ct);
                if (resumeEmbedding.Length > 0)
                    semanticScore = (decimal)CosineSimilarity(jdEmbedding, resumeEmbedding);
            }

            return MapToResult(data, semanticScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Provider} scoring failed — falling back to stub mode. Check the API key and network connectivity.", provider.Name);
            return StubScore(jdText, resumeText, aiCallFailed: true);
        }
    }

    // -- Shared prompt ----------------------------------------------------------

    /// <summary>
    /// The default scoring prompt template.  Callers may supply their own template via
    /// <see cref="ScoreAsync"/>; this constant is also serialised to the database and
    /// returned to the frontend so that users can see and edit the exact prompt beforehand.
    /// Two literal placeholders must be present: <c>{JD_TEXT}</c> and <c>{RESUME_TEXT}</c>.
    /// </summary>
    public static readonly string DefaultPromptTemplate = """
You are an expert resume-to-job-description evaluator.

You will receive:
1. A job description
2. A candidate resume

Your job is to evaluate how well the candidate fits the role using only explicit evidence from the texts.

Instructions:
- Extract candidateName, email, and phone only if explicitly present in the resume. Otherwise use null.
- First identify the JD's core requirements:
  a) required technical skills
  b) required functional/domain skills
  c) required seniority/experience expectations
  d) required qualifications/certifications if explicitly stated
- Then compare the resume against those requirements using only explicit resume evidence.
- Do not assume experience from job titles alone unless the title is supported by responsibilities, tools, or achievements.
- Treat equivalent skills as matches only when equivalence is clear and standard.
- Ignore fluff, self-rating language, and generic claims unless backed by evidence.

Scoring rules:

semanticSimilarityScore (0.0-1.0):
- Based on overall alignment of role, responsibilities, domain, seniority, and demonstrated experience.
- Use 2 decimal precision.

skillsDepthScore (0-100):
- Score how strongly the resume demonstrates the JD's core required skills.
- Weight core required skills more than optional or nice-to-have skills.
- Reward practical implementation evidence more than keyword mentions.

legitimacyScore (0-100):
- Score credibility and consistency of the resume.
- Reduce score only for evidence-based concerns visible in the resume.
- Missing optional details alone are not red flags.

redFlags rules:
- Include only objective concerns supported by visible evidence.
- No speculation.
- If none, return [].

scoreSummary rules:
- 2-3 sentences only.
- Summarize fit level, strongest strengths, and the most important gap or concern if any.
- Do not mention absent contact details or unavailable data.

Output requirements:
- Return ONLY valid JSON.
- No markdown.
- No extra keys.
- Use exactly this structure:

{
  "candidateName": null,
  "email": null,
  "phone": null,
  "semanticSimilarityScore": 0.0,
  "skillsDepthScore": 0,
  "legitimacyScore": 0,
  "scoreSummary": "",
  "skillsMatched": [],
  "skillsGap": [],
  "redFlags": []
}

JOB DESCRIPTION:
{JD_TEXT}

RESUME:
{RESUME_TEXT}
""";

    private static string BuildPrompt(string jdText, string resumeText, string? template = null)
    {
        var t = string.IsNullOrWhiteSpace(template) ? DefaultPromptTemplate : template;
        return t.Replace("{JD_TEXT}", jdText).Replace("{RESUME_TEXT}", resumeText);
    }

    // -- Result mapping ---------------------------------------------------------

    private static ScoringResult MapToResult(LlmScoringData data, decimal semanticScore)
    {
        var overall = semanticScore * SemanticWeight + data.SkillsDepthScore * SkillsDepthWeight + data.LegitimacyScore * LegitimacyWeight;
        return new ScoringResult
        {
            SemanticSimilarityScore = Math.Round(semanticScore, 4),
            SkillsDepthScore        = data.SkillsDepthScore,
            LegitimacyScore         = data.LegitimacyScore,
            OverallScore            = Math.Round(overall, 2),
            Recommendation          = GetRecommendation(overall),
            CandidateName           = NullIfEmpty(data.CandidateName),
            Email                   = NullIfEmpty(data.Email),
            Phone                   = NullIfEmpty(data.Phone),
            ScoreSummary            = NullIfEmpty(data.ScoreSummary),
            SkillsMatched           = data.SkillsMatched,
            SkillsGap               = data.SkillsGap,
            RedFlags                = data.RedFlags.Where(f => !IsEmptyResumeMetaComment(f)).ToList(),
        };
    }

    /// <summary>
    /// Returns true for AI-generated red-flag entries that are meta-commentary about an
    /// empty/unreadable resume rather than a genuine concern about the candidate.
    /// These should not be surfaced to users.
    /// </summary>
    private static bool IsEmptyResumeMetaComment(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return true;
        var lower = flag.Trim().ToLowerInvariant();
        return lower.Contains("completely blank") ||
               lower.Contains("is empty")         ||
               lower.Contains("was empty")        ||
               lower.Contains("empty resume")     ||
               lower.Contains("resume is blank")  ||
               lower.Contains("no content")       ||
               lower.Contains("cannot assess")    ||
               lower.Contains("impossible to assess") ||
               lower.Contains("no resume")        ||
               lower.Contains("provided resume is empty");
    }

    /// <summary>
    /// Returns null for blank strings and for common AI "not found" phrases that
    /// some models return instead of a proper JSON null.
    /// </summary>
    private static string? NullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var lower = value.Trim().ToLowerInvariant();
        if (lower is "n/a" or "na" or "not provided" or "not found" or "not available"
                  or "unknown" or "none" or "null" or "-" or "–")
            return null;
        // Catch phrases like "information was empty", "no information", "not stated", etc.
        if (lower.Contains("not provided") || lower.Contains("not found") ||
            lower.Contains("not available") || lower.Contains("not stated") ||
            lower.Contains("was empty") || lower.Contains("no information") ||
            lower.Contains("not present") || lower.Contains("not listed") ||
            lower.Contains("not mentioned") || lower.Contains("not included"))
            return null;
        return value.Trim();
    }

    // -- Stub fallback ----------------------------------------------------------

    private static ScoringResult StubScore(string jdText, string resumeText, bool aiCallFailed = false)
    {
        var separators = new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?' };

        var jdWords = new HashSet<string>(
            jdText.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                  .Where(w => w.Length > 3)
                  .Select(w => w.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        var resumeWords = resumeText.Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Select(w => w.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matched    = jdWords.Intersect(resumeWords).ToList();
        double ratio   = jdWords.Count > 0 ? (double)matched.Count / jdWords.Count : 0;
        var semantic   = (decimal)(ratio * 0.7 + 0.15);
        var skillsDepth = (decimal)(ratio * 65 + 20);
        var legitimacy = 70m;
        var overall    = semantic * SemanticWeight + skillsDepth * SkillsDepthWeight + legitimacy * LegitimacyWeight;

        return new ScoringResult
        {
            SemanticSimilarityScore = Math.Round(semantic, 4),
            SkillsDepthScore        = Math.Round(skillsDepth, 2),
            LegitimacyScore         = legitimacy,
            OverallScore            = Math.Round(overall, 2),
            Recommendation          = GetRecommendation(overall),
            ScoreSummary            = aiCallFailed
                ? "[Stub mode] AI API call failed — check the API key and server logs. Score based on keyword overlap only."
                : "[Stub mode] Score based on keyword overlap. Configure an AI provider key for real scoring.",
            SkillsMatched           = matched.Take(10).ToList(),
        };
    }

    // -- Shared utilities -------------------------------------------------------

    private static string GetRecommendation(decimal score) =>
        score >= 75 ? "StrongFit" :
        score >= 55 ? "GoodFit"   :
        score >= 35 ? "MaybeFit"  : "NoFit";

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        int len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++) { dot += a[i] * b[i]; magA += a[i] * a[i]; magB += b[i] * b[i]; }
        float mag = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return mag == 0 ? 0 : dot / mag;
    }

    // -- Shared data contract (one class for all providers) ---------------------

    private sealed class LlmScoringData
    {
        public string? CandidateName          { get; set; }
        public string? Email                  { get; set; }
        public string? Phone                  { get; set; }
        public decimal SemanticSimilarityScore { get; set; }
        public decimal SkillsDepthScore        { get; set; }
        public decimal LegitimacyScore         { get; set; }
        public string? ScoreSummary            { get; set; }
        public List<string> SkillsMatched      { get; set; } = [];
        public List<string> SkillsGap          { get; set; } = [];
        public List<string> RedFlags           { get; set; } = [];
    }

    // -- Provider abstraction ---------------------------------------------------

    private interface ILlmProvider
    {
        string Name        { get; }
        bool   IsConfigured { get; }
        Task<LlmScoringData> GetScoringDataAsync(string prompt, CancellationToken ct);
    }

    // -- OpenAI provider --------------------------------------------------------

    private sealed class OpenAiProvider(OpenAiConfiguration cfg, IHttpClientFactory httpClientFactory) : ILlmProvider
    {
        public string Name         => "OpenAI";
        public bool   IsConfigured =>
            !string.IsNullOrWhiteSpace(cfg.ApiKey) && cfg.ApiKey != "REPLACE_WITH_OPENAI_API_KEY";

        public async Task<LlmScoringData> GetScoringDataAsync(string prompt, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new
            {
                model           = cfg.Model,
                response_format = new { type = "json_object" },
                messages        = new[] { new { role = "user", content = prompt } }
            });

            var client = httpClientFactory.CreateClient("OpenAIScoring");
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"OpenAI returned HTTP {(int)response.StatusCode} {response.StatusCode}: {errorBody}",
                    null, response.StatusCode);
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var json = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            return JsonSerializer.Deserialize<LlmScoringData>(json, _jsonOpts) ?? new LlmScoringData();
        }
    }

    // -- Gemini provider --------------------------------------------------------

    private sealed class GeminiProvider(GeminiConfiguration cfg, IHttpClientFactory httpClientFactory) : ILlmProvider
    {
        public string Name         => "Gemini";
        public bool   IsConfigured =>
            !string.IsNullOrWhiteSpace(cfg.ApiKey) && cfg.ApiKey != "REPLACE_WITH_GEMINI_API_KEY";

        public async Task<LlmScoringData> GetScoringDataAsync(string prompt, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new
            {
                contents         = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json" }
            });

            // Use x-goog-api-key header (current auth method per Google docs — ?key= query param is deprecated)
            var client = httpClientFactory.CreateClient("GeminiScoring");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{cfg.Model}:generateContent")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("x-goog-api-key", cfg.ApiKey);

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Gemini returned HTTP {(int)response.StatusCode} {response.StatusCode}: {errorBody}",
                    null, response.StatusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);

            // Gemini wraps JSON output in a text field — strip any markdown code fences if present
            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            var json = StripMarkdownCodeFence(rawText);
            return JsonSerializer.Deserialize<LlmScoringData>(json, _jsonOpts) ?? new LlmScoringData();
        }

        /// <summary>Strips ```json ... ``` fences that Gemini sometimes wraps around JSON output.</summary>
        private static string StripMarkdownCodeFence(string text)
        {
            var t = text.Trim();
            if (t.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = t.IndexOf('\n');
                if (firstNewline >= 0) t = t[(firstNewline + 1)..];
                if (t.EndsWith("```", StringComparison.Ordinal)) t = t[..^3];
            }
            return t.Trim();
        }
    }
}
