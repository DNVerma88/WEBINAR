using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class MentorMentee : BaseEntity
{
    public Guid MentorId { get; set; }
    public Guid MenteeId { get; set; }
    public MentorMenteeStatus Status { get; set; } = MentorMenteeStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? GoalsText { get; set; }
    public string? MatchReason { get; set; }

    public User? Mentor { get; set; }
    public User? Mentee { get; set; }
}
