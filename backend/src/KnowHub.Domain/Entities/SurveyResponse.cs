namespace KnowHub.Domain.Entities;

public class SurveyResponse : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid UserId { get; set; }
    public Guid InvitationId { get; set; }
    public DateTime SubmittedAt { get; set; }

    // Navigation
    public Survey Survey { get; set; } = null!;
    public User User { get; set; } = null!;
    public SurveyInvitation Invitation { get; set; } = null!;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
