using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class ProposalApproval : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid ApproverId { get; set; }
    public ApprovalStep ApprovalStep { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime DecidedAt { get; set; }

    public SessionProposal Proposal { get; set; } = null!;
    public User Approver { get; set; } = null!;
}
