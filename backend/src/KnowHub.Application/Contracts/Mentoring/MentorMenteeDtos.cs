using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class MentorMenteeDto
{
    public Guid Id { get; init; }
    public Guid MentorId { get; init; }
    public string MentorName { get; init; } = string.Empty;
    public Guid MenteeId { get; init; }
    public string MenteeName { get; init; } = string.Empty;
    public MentorMenteeStatus Status { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public string? GoalsText { get; init; }
    public string? MatchReason { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class RequestMentorRequest
{
    public Guid MentorId { get; set; }
    public string? GoalsText { get; set; }
}
