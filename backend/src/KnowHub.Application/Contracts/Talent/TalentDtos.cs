using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts.Talent;

public record ResumeProfileDto(
    Guid Id,
    Guid UserId,
    string Template,
    string PersonalInfo,
    string? Summary,
    string WorkExperience,
    string Education,
    string Skills,
    string Certifications,
    string Projects,
    string Languages,
    string Publications,
    string Achievements,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record ResumeProfileAdminSummaryDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Department,
    string? Designation,
    bool HasProfile,
    DateTimeOffset? UpdatedAt
);

public record SaveResumeProfileRequest(
    string Template,
    string PersonalInfo,
    string? Summary,
    string WorkExperience,
    string Education,
    string Skills,
    string Certifications,
    string Projects,
    string Languages,
    string Publications,
    string Achievements
);

// -- Resume Import (AI-parsed output; returned to frontend for review before saving) ----------

public record ParsedResumeDto(
    ParsedPersonalInfoDto     PersonalInfo,
    string?                   Summary,
    IReadOnlyList<ParsedWorkExperienceDto>  WorkExperience,
    IReadOnlyList<ParsedEducationDto>       Education,
    IReadOnlyList<ParsedSkillDto>           Skills,
    IReadOnlyList<ParsedCertificationDto>   Certifications,
    IReadOnlyList<ParsedProjectDto>         Projects,
    IReadOnlyList<ParsedLanguageDto>        Languages,
    IReadOnlyList<ParsedPublicationDto>     Publications,
    IReadOnlyList<ParsedAchievementDto>     Achievements
);

public record ParsedPersonalInfoDto(
    string? FullName, string? Email, string? Phone, string? Location,
    string? LinkedIn, string? Website, string? Headline);

public record ParsedWorkExperienceDto(
    string? JobTitle, string? Company, string? StartDate, string? EndDate, string? Description);

public record ParsedEducationDto(
    string? Degree, string? Institution, string? StartYear, string? EndYear);

public record ParsedSkillDto(string? Name, string? Level);

public record ParsedCertificationDto(string? Name, string? Issuer, string? Date, string? Url);

public record ParsedProjectDto(
    string? Name, string? Company, string? Description, string? Technologies, string? Url);

public record ParsedLanguageDto(string? Name, string? Proficiency);

public record ParsedPublicationDto(string? Title, string? Journal, string? Year, string? Url);

public record ParsedAchievementDto(string? Title, string? Year, string? Description);

public record ScreeningJobDto(
    Guid Id,
    Guid TenantId,
    Guid CreatedByUserId,
    string JobTitle,
    string? JdText,
    ScreeningJobStatus Status,
    int TotalCandidates,
    int ProcessedCandidates,
    int ProgressPercent,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt
);

public record ScreeningCandidateDto(
    Guid Id,
    Guid ScreeningJobId,
    string FileName,
    string StorageProviderType,
    CandidateStatus Status,
    string? ErrorMessage,
    string? CandidateName,
    string? Email,
    string? Phone,
    decimal? SemanticSimilarityScore,
    decimal? SkillsDepthScore,
    decimal? LegitimacyScore,
    decimal? OverallScore,
    string? Recommendation,
    string? ScoreSummary,
    IReadOnlyList<string>? SkillsMatched,
    IReadOnlyList<string>? SkillsGap,
    IReadOnlyList<string>? RedFlags,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScoredAt
);

public record ScreeningJobDetailDto(
    Guid Id,
    Guid TenantId,
    Guid CreatedByUserId,
    string JobTitle,
    string? JdText,
    string? PromptTemplate,
    ScreeningJobStatus Status,
    int TotalCandidates,
    int ProcessedCandidates,
    int ProgressPercent,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ScreeningCandidateDto> Candidates
);

public record CreateScreeningJobRequest(
    string JobTitle,
    string? JdText,
    StorageFileReference? JdFileReference,
    string? PromptTemplate = null
);

/// <summary>
/// When <c>OverwriteAllScores</c> is <c>false</c> (the default), only candidates that
/// are in <c>Failed</c> or <c>Queued</c> status are re-queued — already-scored candidates
/// are left untouched. Set <c>true</c> to force a full re-score of every candidate.
/// </summary>
public record ReScreenRequest(string ScoringMode, bool OverwriteAllScores = false, string? PromptTemplate = null);

public record UpdateJdRequest(string JdText);

/// <summary>Optional body for POST {jobId}/start. If omitted the default scoring mode is used.</summary>
public record StartScreeningRequest(string ScoringMode = ScoringModes.Gemini, string? PromptTemplate = null);

/// <summary>Reports which AI providers are currently configured in the running instance.</summary>
public record AiProviderStatusDto(
    bool OpenAiConfigured,
    bool GeminiConfigured,
    string OpenAiModel,
    string GeminiModel
);

public record UploadedFileInfo(
    string FileName,
    string LocalFilePath,
    long FileSizeBytes
);

// -- Inner DTOs for JSON serialised fields in ResumeProfile -------------------

public record PersonalInfoDto(
    string? FullName,
    string? Email,
    string? Phone,
    string? Location,
    string? LinkedIn,
    string? Website,
    string? Headline = null   // job title / headline shown below the name
);

public record WorkExperienceDto(
    string? JobTitle,
    string? Company,
    string? StartDate,
    string? EndDate,
    string? Description
);

public record EducationDto(
    string? Degree,
    string? Institution,
    string? StartYear,
    string? EndYear
);

public record SkillDto(
    string Name,
    string? Level
);

public record CertificationDto(
    string? Name,
    string? Issuer,           // matches frontend `issuer`
    string? Date,             // matches frontend `date`
    string? Url               // matches frontend `url`
);

public record ProjectDto(
    string? Name,             // matches frontend `name`
    string? Description,
    string? Technologies,
    string? Url,              // matches frontend `url`
    string? Company = null    // matches frontend `company` (for PROJECT DETAILS grouping)
);

public record LanguageDto(
    string? Name,
    string? Proficiency
);

public record PublicationDto(
    string? Title,
    string? Journal,          // matches frontend `journal`
    string? Year,             // matches frontend `year`
    string? Url
);

public record AchievementDto(
    string? Title,
    string? Year,
    string? Description
);

// -- Screening storage file ref (API body DTO, mirrors StorageFileReference) --

public record StorageFileRef(
    string ProviderType,
    string FileId,
    string FileName,
    long FileSizeBytes,
    string? ContainerOrDrive = null,
    string? AccessToken = null
);

public record AddStorageFilesRequest(List<StorageFileRef> Files);

// -- Screening summary / detail / result views ---------------------------------

public record ScreeningJobListDto(
    Guid Id,
    string JobTitle,
    string Status,
    int TotalCandidates,
    int ProcessedCandidates,
    int ProgressPercent,
    DateTimeOffset CreatedAt
);

public record ScreeningResultDto(
    Guid CandidateId,
    string FileName,
    string? CandidateName,
    string? Email,
    string? Phone,
    decimal? OverallScore,
    decimal? SemanticSimilarityScore,
    decimal? SkillsDepthScore,
    decimal? LegitimacyScore,
    string? Recommendation,
    string? ScoreSummary,
    string? SkillsMatched,
    string? SkillsGap,
    string? RedFlags
);
