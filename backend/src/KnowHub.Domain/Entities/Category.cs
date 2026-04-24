namespace KnowHub.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SessionProposal> Proposals { get; set; } = new List<SessionProposal>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
