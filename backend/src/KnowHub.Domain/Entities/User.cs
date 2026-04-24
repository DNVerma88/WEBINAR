using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Location { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Employee;
    public bool IsActive { get; set; } = true;
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public ContributorProfile? ContributorProfile { get; set; }
    public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
    public ICollection<UserFollower> Followers { get; set; } = new List<UserFollower>();
    public ICollection<UserFollower> Following { get; set; } = new List<UserFollower>();
    public ICollection<SessionProposal> Proposals { get; set; } = new List<SessionProposal>();
    public ICollection<SessionRegistration> Registrations { get; set; } = new List<SessionRegistration>();
    public ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();
    public ICollection<CommunityMember> CommunityMemberships { get; set; } = new List<CommunityMember>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
