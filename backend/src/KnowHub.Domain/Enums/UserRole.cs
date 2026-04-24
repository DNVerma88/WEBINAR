namespace KnowHub.Domain.Enums;

[Flags]
public enum UserRole
{
    None = 0,
    Employee = 1,
    Contributor = 2,
    Manager = 4,
    KnowledgeTeam = 8,
    Admin = 16,
    SuperAdmin = 32
}
