namespace KnowHub.Domain.Enums;

public enum AssessmentPeriodFrequency
{
    Weekly     = 0,
    BiWeekly   = 1,
    Monthly    = 2,
    Quarterly  = 3,
    HalfYearly = 4,
    Annual     = 5
}

public enum AssessmentPeriodStatus
{
    Draft     = 0,
    Open      = 1,
    Closed    = 2,
    Published = 3
}

public enum AssessmentStatus
{
    Draft     = 0,
    Submitted = 1,
    Reopened  = 2
}

public enum AssessmentActionType
{
    Created          = 0,
    Updated          = 1,
    Submitted        = 2,
    Reopened         = 3,
    PrimaryLeadChanged = 4,
    CoLeadAssigned     = 5,
    CoLeadRemoved      = 6,
    MemberAssigned     = 7,
    MemberRemoved      = 8,
    PeriodOpened     = 9,
    PeriodClosed     = 10,
    PeriodPublished  = 11
}
