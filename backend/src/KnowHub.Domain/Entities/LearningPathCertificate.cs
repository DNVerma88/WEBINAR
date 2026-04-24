namespace KnowHub.Domain.Entities;

public class LearningPathCertificate : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LearningPathId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string CertificateUrl { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }

    public User? User { get; set; }
    public LearningPath? LearningPath { get; set; }
}
