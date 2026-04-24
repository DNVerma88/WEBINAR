namespace KnowHub.Domain.Enums;

public enum SurveyStatus
{
    Draft  = 0, // Survey is being configured; not visible to employees
    Active = 1, // Survey is live; invitations have been sent
    Closed = 2, // Survey is no longer accepting responses
}
