using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class Survey : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? ThankYouMessage { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    public int TokenExpiryDays { get; set; } = 7;  // kept as fallback when EndsAt is null
    public DateTime? EndsAt { get; set; }
    public bool IsAnonymous { get; set; } = false;
    public DateTime? LaunchedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int TotalInvited { get; set; } = 0;
    public int TotalResponded { get; set; } = 0;

    // Navigation
    public ICollection<SurveyQuestion>   Questions   { get; set; } = new List<SurveyQuestion>();
    public ICollection<SurveyInvitation> Invitations { get; set; } = new List<SurveyInvitation>();
    public ICollection<SurveyResponse>   Responses   { get; set; } = new List<SurveyResponse>();
}
