namespace KnowHub.Application.Contracts;

public class AfterActionReviewDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string WhatWasPlanned { get; init; } = string.Empty;
    public string WhatHappened { get; init; } = string.Empty;
    public string WhatWentWell { get; init; } = string.Empty;
    public string WhatToImprove { get; init; } = string.Empty;
    public string KeyLessonsLearned { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class CreateAarRequest
{
    public string WhatWasPlanned { get; set; } = string.Empty;
    public string WhatHappened { get; set; } = string.Empty;
    public string WhatWentWell { get; set; } = string.Empty;
    public string WhatToImprove { get; set; } = string.Empty;
    public string KeyLessonsLearned { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

public class UpdateAarRequest
{
    public string WhatWasPlanned { get; set; } = string.Empty;
    public string WhatHappened { get; set; } = string.Empty;
    public string WhatWentWell { get; set; } = string.Empty;
    public string WhatToImprove { get; set; } = string.Empty;
    public string KeyLessonsLearned { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}
