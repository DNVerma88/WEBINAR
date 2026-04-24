namespace KnowHub.Application.Contracts;

public class SkillEndorsementDto
{
    public Guid Id { get; init; }
    public Guid EndorserId { get; init; }
    public string EndorserName { get; init; } = string.Empty;
    public Guid EndorseeId { get; init; }
    public Guid TagId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public Guid SessionId { get; init; }
    public DateTime EndorsedAt { get; init; }
}

public class EndorseSkillRequest
{
    public Guid EndorseeId { get; set; }
    public Guid TagId { get; set; }
}
