using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class LeaderboardSnapshot : BaseEntity
{
    public int SnapshotMonth { get; set; }
    public int SnapshotYear { get; set; }
    public LeaderboardType LeaderboardType { get; set; }
    public string Entries { get; set; } = "[]";
    public DateTime GeneratedAt { get; set; }
}
