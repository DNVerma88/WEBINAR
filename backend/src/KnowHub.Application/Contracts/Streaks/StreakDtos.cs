namespace KnowHub.Application.Contracts;

public class UserStreakDto
{
    public Guid UserId { get; init; }
    public int CurrentStreakDays { get; init; }
    public int LongestStreakDays { get; init; }
    public DateOnly LastActivityDate { get; init; }
    public DateOnly? StreakFrozenUntil { get; init; }
}
