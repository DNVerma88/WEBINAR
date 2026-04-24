namespace KnowHub.Domain.Entities.Talent;

public class ResumeProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Template { get; set; } = "Professional";
    public string PersonalInfo { get; set; } = "{}";   // JSONB stored as string
    public string? Summary { get; set; }
    public string WorkExperience { get; set; } = "[]";
    public string Education { get; set; } = "[]";
    public string Skills { get; set; } = "[]";
    public string Certifications { get; set; } = "[]";
    public string Projects { get; set; } = "[]";
    public string Languages { get; set; } = "[]";
    public string Publications { get; set; } = "[]";
    public string Achievements { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
