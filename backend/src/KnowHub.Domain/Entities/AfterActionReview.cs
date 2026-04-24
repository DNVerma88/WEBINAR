namespace KnowHub.Domain.Entities;

public class AfterActionReview : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid AuthorId { get; set; }
    public string WhatWasPlanned { get; set; } = string.Empty;
    public string WhatHappened { get; set; } = string.Empty;
    public string WhatWentWell { get; set; } = string.Empty;
    public string WhatToImprove { get; set; } = string.Empty;
    public string KeyLessonsLearned { get; set; } = string.Empty;
    public bool IsPublished { get; set; }

    public Session? Session { get; set; }
    public User? Author { get; set; }
}
