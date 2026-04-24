using KnowHub.Application.Models;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class ContentReportDto
{
    public Guid Id { get; init; }
    public string ReporterName { get; init; } = string.Empty;
    public Guid? TargetPostId { get; init; }
    public string? TargetPostTitle { get; init; }
    public Guid? TargetCommentId { get; init; }
    public ReportReason ReasonCode { get; init; }
    public string? Description { get; init; }
    public ReportStatus Status { get; init; }
    public string? ResolverName { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime CreatedDate { get; init; }
}

// ─── Requests ─────────────────────────────────────────────────────

public class ReportContentRequest
{
    public Guid? TargetPostId { get; set; }
    public Guid? TargetCommentId { get; set; }
    public ReportReason ReasonCode { get; set; }
    public string? Description { get; set; }
}

public class ResolveReportRequest
{
    public string? ModeratorNote { get; set; }
}

// ─── Interface ────────────────────────────────────────────────────

public interface IContentModerationService
{
    Task ReportContentAsync(ReportContentRequest request, CancellationToken ct);
    Task<PagedResult<ContentReportDto>> GetOpenReportsAsync(int pageNumber, int pageSize, CancellationToken ct);
    Task ResolveReportAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct);
    Task DismissReportAsync(Guid reportId, CancellationToken ct);
}
