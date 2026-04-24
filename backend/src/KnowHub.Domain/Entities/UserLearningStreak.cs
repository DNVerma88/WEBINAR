namespace KnowHub.Domain.Entities;

public class UserLearningStreak : BaseEntity
{
    public Guid UserId { get; set; }
    public int CurrentStreakDays { get; set; }
    public int LongestStreakDays { get; set; }
    public DateOnly LastActivityDate { get; set; }
    public DateOnly? StreakFrozenUntil { get; set; }

    public User? User { get; set; }
}
