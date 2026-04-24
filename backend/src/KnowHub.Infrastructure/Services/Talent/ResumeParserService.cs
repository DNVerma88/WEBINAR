using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Services.Talent;

/// <summary>
/// Parses an uploaded PDF or DOCX resume in-memory using an AI provider (Gemini â†’ OpenAI â†’ Stub)
/// and returns a <see cref="ParsedResumeDto"/> ready for the frontend to pre-fill the resume form.
/// No file is persisted to disk; the stream is consumed and discarded.
/// </summary>
public class ResumeParserService : IResumeParserService
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly AiConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ResumeParserService> _logger;

    public ResumeParserService(
        IOptions<AiConfiguration> config,
        IHttpClientFactory httpClientFactory,
        ILogger<ResumeParserService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // -- Public API -------------------------------------------------------------

    public async Task<ParsedResumeDto?> ParseAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        // OWASP A03 â€” validate magic bytes to prevent content-type spoofing before any processing
        if (!ResumeTextExtractor.IsValidFileType(fileStream, fileName))
        {
            _logger.LogWarning("Resume import rejected: magic-byte validation failed for '{FileName}'.", fileName);
            return null;
        }

        // Extract plain text in-memory â€” nothing written to disk
        string resumeText;
        try
        {
            resumeText = ResumeTextExtractor.ExtractText(fileStream, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Text extraction failed for '{FileName}'.", fileName);
            return null;
        }

        if (string.IsNullOrWhiteSpace(resumeText))
        {
            _logger.LogWarning("No text could be extracted from '{FileName}'.", fileName);
            return null;
        }

        IAiParserProvider provider = SelectProvider();
        _logger.LogInformation("Parsing resume '{FileName}' with {Provider}.", fileName, provider.Name);

        string prompt = BuildPrompt(resumeText);
        ParsedResumeRaw? raw;
        try
        {
            raw = await provider.ParseAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Provider} resume parsing failed for '{FileName}'.", provider.Name, fileName);
            return null;
        }

        return raw is null ? null : MapToDto(raw);
    }

    // -- Provider selection -----------------------------------------------------

    /// <summary>
    /// Returns the appropriate AI parser provider: Gemini â†’ OpenAI â†’ Stub.
    /// </summary>
    private IAiParserProvider SelectProvider()
    {
        _logger.LogInformation("Using AI provider for resume import.");
        var gemini = new GeminiParserProvider(_config.Gemini, _httpClientFactory);
        if (gemini.IsConfigured) return gemini;

        var openAi = new OpenAiParserProvider(_config.OpenAI, _httpClientFactory);
        if (openAi.IsConfigured) return openAi;

        _logger.LogWarning("No AI provider configured â€” falling back to stub. Configure Gemini or OpenAI for AI-powered parsing.");
        return new StubParserProvider();
    }

    // -- Prompt -----------------------------------------------------------------

    private static string BuildPrompt(string resumeText)
    {
        const string schema = """
{
  "personalInfo": {
    "fullName": null, "email": null, "phone": null, "location": null,
    "linkedIn": null, "website": null, "headline": null
  },
  "summary": null,
  "workExperience": [{"jobTitle": null, "company": null, "startDate": null, "endDate": null, "description": null}],
  "education": [{"degree": null, "institution": null, "startYear": null, "endYear": null}],
  "skills": [{"name": null, "level": null}],
  "certifications": [{"name": null, "issuer": null, "date": null, "url": null}],
  "projects": [{"name": null, "company": null, "description": null, "technologies": null, "url": null}],
  "languages": [{"name": null, "proficiency": null}],
  "publications": [{"title": null, "journal": null, "year": null, "url": null}],
  "achievements": [{"title": null, "year": null, "description": null}]
}
""";
        return $"""
You are a professional resume parser. Extract ALL information from the resume text below
and return ONLY a valid JSON object with exactly this structure.
Use null for any missing scalar fields. Use empty arrays [] for missing sections.
Do NOT include markdown fences, code blocks, or any explanation â€” return raw JSON only.

IMPORTANT mapping rules:
- Any section labelled "Awards", "Honors", "Honours", "Recognition", "Accomplishments",
  "Distinctions", "Awards & Recognition", or "Awards & Achievements" must be mapped to the
  "achievements" array â€” never left out.
- Any section labelled "Certifications", "Certificates", "Trainings", "Certifications / Trainings",
  "Courses", or "Licenses" must be mapped to the "certifications" array.
- Extract ALL bullet points and lines found in each section; do not trim or summarise.

{schema}
--- RESUME TEXT START ---
{resumeText}
--- RESUME TEXT END ---
""";
    }

    // -- DTO mapping ------------------------------------------------------------

    private static ParsedResumeDto MapToDto(ParsedResumeRaw raw) => new(
        PersonalInfo: new ParsedPersonalInfoDto(
            raw.PersonalInfo?.FullName,
            raw.PersonalInfo?.Email,
            raw.PersonalInfo?.Phone,
            raw.PersonalInfo?.Location,
            raw.PersonalInfo?.LinkedIn,
            raw.PersonalInfo?.Website,
            raw.PersonalInfo?.Headline),
        Summary: raw.Summary,
        WorkExperience: (raw.WorkExperience ?? [])
            .Select(x => new ParsedWorkExperienceDto(x.JobTitle, x.Company, x.StartDate, x.EndDate, x.Description))
            .ToList(),
        Education: (raw.Education ?? [])
            .Select(x => new ParsedEducationDto(x.Degree, x.Institution, x.StartYear, x.EndYear))
            .ToList(),
        Skills: (raw.Skills ?? [])
            .Select(x => new ParsedSkillDto(x.Name, x.Level))
            .ToList(),
        Certifications: (raw.Certifications ?? [])
            .Select(x => new ParsedCertificationDto(x.Name, x.Issuer, x.Date, x.Url))
            .ToList(),
        Projects: (raw.Projects ?? [])
            .Select(x => new ParsedProjectDto(x.Name, x.Company, x.Description, x.Technologies, x.Url))
            .ToList(),
        Languages: (raw.Languages ?? [])
            .Select(x => new ParsedLanguageDto(x.Name, x.Proficiency))
            .ToList(),
        Publications: (raw.Publications ?? [])
            .Select(x => new ParsedPublicationDto(x.Title, x.Journal, x.Year, x.Url))
            .ToList(),
        Achievements: (raw.Achievements ?? [])
            .Select(x => new ParsedAchievementDto(x.Title, x.Year, x.Description))
            .ToList()
    );

    // -- Internal deserialization classes (raw AI JSON â†’ typed) -----------------

    private sealed class ParsedResumeRaw
    {
        public RawPersonalInfo?           PersonalInfo   { get; set; }
        public string?                    Summary        { get; set; }
        public List<RawWorkExperience>?   WorkExperience { get; set; }
        public List<RawEducation>?        Education      { get; set; }
        public List<RawSkill>?            Skills         { get; set; }
        public List<RawCertification>?    Certifications { get; set; }
        public List<RawProject>?          Projects       { get; set; }
        public List<RawLanguage>?         Languages      { get; set; }
        public List<RawPublication>?      Publications   { get; set; }
        public List<RawAchievement>?      Achievements   { get; set; }
    }

    private sealed class RawPersonalInfo
    {
        public string? FullName  { get; set; }
        public string? Email     { get; set; }
        public string? Phone     { get; set; }
        public string? Location  { get; set; }
        public string? LinkedIn  { get; set; }
        public string? Website   { get; set; }
        public string? Headline  { get; set; }
    }

    private sealed class RawWorkExperience
    {
        public string? JobTitle     { get; set; }
        public string? Company      { get; set; }
        public string? StartDate    { get; set; }
        public string? EndDate      { get; set; }
        public string? Description  { get; set; }
    }

    private sealed class RawEducation
    {
        public string? Degree       { get; set; }
        public string? Institution  { get; set; }
        public string? StartYear    { get; set; }
        public string? EndYear      { get; set; }
    }

    private sealed class RawSkill
    {
        public string? Name  { get; set; }
        public string? Level { get; set; }
    }

    private sealed class RawCertification
    {
        public string? Name   { get; set; }
        public string? Issuer { get; set; }
        public string? Date   { get; set; }
        public string? Url    { get; set; }
    }

    private sealed class RawProject
    {
        public string? Name          { get; set; }
        public string? Company       { get; set; }
        public string? Description   { get; set; }
        public string? Technologies  { get; set; }
        public string? Url           { get; set; }
    }

    private sealed class RawLanguage
    {
        public string? Name        { get; set; }
        public string? Proficiency { get; set; }
    }

    private sealed class RawPublication
    {
        public string? Title   { get; set; }
        public string? Journal { get; set; }
        public string? Year    { get; set; }
        public string? Url     { get; set; }
    }

    private sealed class RawAchievement
    {
        public string? Title       { get; set; }
        public string? Year        { get; set; }
        public string? Description { get; set; }
    }

    // -- Provider abstraction (Strategy pattern) --------------------------------

    private interface IAiParserProvider
    {
        string Name        { get; }
        bool   IsConfigured { get; }
        Task<ParsedResumeRaw?> ParseAsync(string prompt, CancellationToken ct);
    }

    // -- Gemini provider --------------------------------------------------------

    private sealed class GeminiParserProvider(GeminiConfiguration cfg, IHttpClientFactory httpClientFactory)
        : IAiParserProvider
    {
        public string Name         => "Gemini";
        public bool   IsConfigured =>
            !string.IsNullOrWhiteSpace(cfg.ApiKey) && cfg.ApiKey != "REPLACE_WITH_GEMINI_API_KEY";

        public async Task<ParsedResumeRaw?> ParseAsync(string prompt, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new
            {
                contents         = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json" }
            });

            // Reuse named client already registered for scoring (same base URL + timeout)
            var client = httpClientFactory.CreateClient("GeminiScoring");
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"v1beta/models/{cfg.Model}:generateContent")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("x-goog-api-key", cfg.ApiKey);

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Gemini returned HTTP {(int)response.StatusCode}: {errorBody}",
                    null, response.StatusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);
            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            return JsonSerializer.Deserialize<ParsedResumeRaw>(StripMarkdownCodeFence(rawText), _jsonOpts);
        }

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

    // -- OpenAI provider --------------------------------------------------------

    private sealed class OpenAiParserProvider(OpenAiConfiguration cfg, IHttpClientFactory httpClientFactory)
        : IAiParserProvider
    {
        public string Name         => "OpenAI";
        public bool   IsConfigured =>
            !string.IsNullOrWhiteSpace(cfg.ApiKey) && cfg.ApiKey != "REPLACE_WITH_OPENAI_API_KEY";

        public async Task<ParsedResumeRaw?> ParseAsync(string prompt, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new
            {
                model           = cfg.Model,
                response_format = new { type = "json_object" },
                messages        = new[] { new { role = "user", content = prompt } }
            });

            // Reuse named client already registered for scoring (same base URL + timeout)
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
                    $"OpenAI returned HTTP {(int)response.StatusCode}: {errorBody}",
                    null, response.StatusCode);
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var json = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            return JsonSerializer.Deserialize<ParsedResumeRaw>(json, _jsonOpts);
        }
    }

    // -- Stub fallback (no AI key configured) ----------------------------------

    private sealed class StubParserProvider : IAiParserProvider
    {
        public string Name         => "Stub";
        public bool   IsConfigured => true;

        public Task<ParsedResumeRaw?> ParseAsync(string prompt, CancellationToken ct) =>
            Task.FromResult<ParsedResumeRaw?>(new ParsedResumeRaw
            {
                PersonalInfo   = new RawPersonalInfo(),
                Summary        = "[AI provider not configured â€” fill in details manually]",
                WorkExperience = [],
                Education      = [],
                Skills         = [],
                Certifications = [],
                Projects       = [],
                Languages      = [],
                Publications   = [],
                Achievements   = []
            });
    }

}
