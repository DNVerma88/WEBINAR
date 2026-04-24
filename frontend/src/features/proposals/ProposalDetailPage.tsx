import { Alert, Box, Button, Chip, Divider, Paper, Stack, TextField, Typography } from '@/components/ui';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { proposalsApi } from '../../shared/api/proposals';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import { ProposalStatus, DifficultyLevel, SessionFormat, UserRole, PROPOSAL_STATUS_LABELS, DIFFICULTY_LEVEL_LABELS, SESSION_FORMAT_LABELS } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';

export default function ProposalDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { user, hasRole } = useAuth();
  const toast = useToast();

  const [rejectComment, setRejectComment] = useState('');
  const [revisionComment, setRevisionComment] = useState('');
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [showRevisionDialog, setShowRevisionDialog] = useState(false);
  const [showApproveDialog, setShowApproveDialog] = useState(false);
  const [showSubmitDialog, setShowSubmitDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const { data: proposal, isLoading, error } = useQuery({
    queryKey: ['proposals', id],
    queryFn: ({ signal }) => proposalsApi.getProposalById(id!, signal),
    enabled: !!id,
  });

  usePageTitle(proposal?.title ?? 'Proposal');

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['proposals'] });
  };

  const submitMutation = useMutation({
    mutationFn: () => proposalsApi.submitProposal(id!),
    onSuccess: () => { invalidate(); toast.success('Proposal submitted for review.'); },
    onError: () => toast.error('Failed to submit proposal.'),
  });
  const approveMutation = useMutation({
    mutationFn: () => proposalsApi.approveProposal(id!),
    onSuccess: () => { setShowApproveDialog(false); invalidate(); toast.success('Proposal approved.'); },
    onError: () => { setShowApproveDialog(false); toast.error('Failed to approve proposal.'); },
  });
  const rejectMutation = useMutation({
    mutationFn: () => proposalsApi.rejectProposal(id!, { comment: rejectComment }),
    onSuccess: () => { setShowRejectDialog(false); invalidate(); toast.success('Proposal rejected.'); },
    onError: () => { setShowRejectDialog(false); toast.error('Failed to reject proposal.'); },
  });
  const revisionMutation = useMutation({
    mutationFn: () => proposalsApi.requestRevision(id!, { comment: revisionComment }),
    onSuccess: () => { setShowRevisionDialog(false); invalidate(); toast.success('Revision requested.'); },
    onError: () => { setShowRevisionDialog(false); toast.error('Failed to request revision.'); },
  });

  const navigate = useNavigate();
  const deleteMutation = useMutation({
    mutationFn: () => proposalsApi.deleteProposal(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['proposals'] }); navigate('/proposals'); },
    onError: () => toast.error('Failed to delete proposal.'),
  });

  const isOwner = proposal?.proposerId === user?.userId;
  const isManager = hasRole(UserRole.Manager);
  const isKnowledgeTeam = hasRole(UserRole.KnowledgeTeam);
  const isAdmin = hasRole(UserRole.Admin);
  const isSuperAdmin = hasRole(UserRole.SuperAdmin);
  const isAdminOrAbove = isAdmin || isSuperAdmin;

  const canSubmit = isOwner && proposal?.status === ProposalStatus.Draft;
  const canResubmit = isOwner && proposal?.status === ProposalStatus.RevisionRequested;
  const canEditRole = isOwner || isAdminOrAbove || isManager || isKnowledgeTeam;
  const canEdit = canEditRole && (proposal?.status === ProposalStatus.Draft || proposal?.status === ProposalStatus.RevisionRequested);
  const canDelete = (isOwner || isAdminOrAbove) && (proposal?.status === ProposalStatus.Draft || proposal?.status === ProposalStatus.RevisionRequested);

  const canApproveAsManager =
    (isManager || isAdminOrAbove) && proposal?.status === ProposalStatus.ManagerReview;
  const canApproveAsKT =
    (isKnowledgeTeam || isAdminOrAbove) && proposal?.status === ProposalStatus.KnowledgeTeamReview;
  // AdminOrAbove can approve/reject at ANY review stage
  const canApproveAnyStage = isAdminOrAbove && (
    proposal?.status === ProposalStatus.Submitted ||
    proposal?.status === ProposalStatus.ManagerReview ||
    proposal?.status === ProposalStatus.KnowledgeTeamReview
  );
  const canApprove = canApproveAsManager || canApproveAsKT || canApproveAnyStage;

  const canRejectOrRevise =
    ((isManager || isAdminOrAbove) && proposal?.status === ProposalStatus.ManagerReview) ||
    ((isKnowledgeTeam || isAdminOrAbove) && proposal?.status === ProposalStatus.KnowledgeTeamReview) ||
    (isAdminOrAbove && proposal?.status === ProposalStatus.Submitted);

  if (isLoading) return <LoadingOverlay />;
  if (!proposal) return <Typography>Proposal not found.</Typography>;

  return (
    <Box>
      <PageHeader
        title={proposal.title}
        breadcrumbs={[{ label: 'Proposals', to: '/proposals' }, { label: proposal.title }]}
        actions={
          canEdit ? (
            <Stack direction="row" spacing={1}>
              <Button variant="outlined" component={Link} to={`/proposals/${id}/edit`}>
                Edit
              </Button>
              {canDelete && (
                <Button variant="outlined" color="error" onClick={() => setShowDeleteDialog(true)}>
                  Delete
                </Button>
              )}
            </Stack>
          ) : undefined
        }
      />

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={submitMutation.error} />
      <ApiErrorAlert error={approveMutation.error} />
      <ApiErrorAlert error={rejectMutation.error} />
      <ApiErrorAlert error={revisionMutation.error} />

      {proposal.status === ProposalStatus.RevisionRequested && isOwner && (
        <Alert severity="warning" sx={{ mb: 2, maxWidth: 720 }}>
          A revision has been requested. Please click <strong>Edit</strong> to update your proposal, then <strong>Submit for Review</strong> again.
        </Alert>
      )}
      {proposal.status === ProposalStatus.Submitted && isOwner && (
        <Alert severity="info" sx={{ mb: 2, maxWidth: 720 }}>
          Your proposal is currently under review. You cannot edit it until a reviewer requests changes.
        </Alert>
      )}

      <Paper sx={{ p: 3, maxWidth: 720 }}>
        <Stack direction="row" spacing={1} mb={2} flexWrap="wrap">
          <Chip label={PROPOSAL_STATUS_LABELS[proposal.status as ProposalStatus] ?? proposal.status} size="small" />
          <Chip label={SESSION_FORMAT_LABELS[proposal.format as SessionFormat] ?? proposal.format} size="small" variant="outlined" />
          <Chip label={DIFFICULTY_LEVEL_LABELS[proposal.difficultyLevel as DifficultyLevel] ?? proposal.difficultyLevel} size="small" variant="outlined" />
        </Stack>

        <Typography variant="body2" color="text.secondary" gutterBottom>
          Proposed by <strong>{proposal.proposerName}</strong> · Category: <strong>{proposal.categoryName}</strong>
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Topic: <strong>{proposal.topic}</strong> · Duration: <strong>{proposal.estimatedDurationMinutes} min</strong>
        </Typography>

        {proposal.description && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle2" fontWeight={600} gutterBottom>Description</Typography>
            <Typography variant="body2" color="text.secondary">{proposal.description}</Typography>
          </>
        )}

        {proposal.targetAudience && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle2" fontWeight={600} gutterBottom>Target Audience</Typography>
            <Typography variant="body2" color="text.secondary">{proposal.targetAudience}</Typography>
          </>
        )}

        {proposal.prerequisites && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle2" fontWeight={600} gutterBottom>Prerequisites</Typography>
            <Typography variant="body2" color="text.secondary">{proposal.prerequisites}</Typography>
          </>
        )}

        {proposal.expectedOutcomes && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle2" fontWeight={600} gutterBottom>Expected Outcomes</Typography>
            <Typography variant="body2" color="text.secondary">{proposal.expectedOutcomes}</Typography>
          </>
        )}

        <Divider sx={{ my: 3 }} />

        {/* Actions */}
        <Stack direction="row" spacing={2} flexWrap="wrap">
          {(canSubmit || canResubmit) && (
            <Button
              variant="contained"
              onClick={() => setShowSubmitDialog(true)}
              disabled={submitMutation.isPending}
            >
              Submit for Review
            </Button>
          )}
          {canApprove && (
            <Button
              variant="contained"
              color="success"
              onClick={() => setShowApproveDialog(true)}
            >
              Approve
            </Button>
          )}
          {canRejectOrRevise && (
            <>
              <Button variant="outlined" color="warning" onClick={() => setShowRevisionDialog(true)}>
                Request Revision
              </Button>
              <Button variant="outlined" color="error" onClick={() => setShowRejectDialog(true)}>
                Reject
              </Button>
            </>
          )}
        </Stack>
      </Paper>

      {/* Submit Dialog */}
      <ConfirmDialog
        open={showSubmitDialog}
        title="Submit Proposal for Review"
        message={`Your proposal "${proposal.title}" will be submitted for review. Make sure all information is complete and up to date — you won't be able to edit it once submitted unless a reviewer requests changes.`}
        confirmLabel="Submit"
        onConfirm={() => submitMutation.mutate()}
        onCancel={() => setShowSubmitDialog(false)}
        loading={submitMutation.isPending}
      />

      {/* Approve Dialog */}
      <ConfirmDialog
        open={showApproveDialog}
        title="Approve Proposal"
        message="Are you sure you want to approve this proposal? It will move to the next review stage or be published."
        confirmLabel="Approve"
        onConfirm={() => approveMutation.mutate()}
        onCancel={() => setShowApproveDialog(false)}
        loading={approveMutation.isPending}
      />

      {/* Reject Dialog */}
      <ConfirmDialog
        open={showRejectDialog}
        title="Reject Proposal"
        message=""
        confirmLabel="Reject"
        onConfirm={() => rejectMutation.mutate()}
        onCancel={() => setShowRejectDialog(false)}
        loading={rejectMutation.isPending}
        danger
      >
        <TextField
          label="Reason for rejection"
          multiline
          rows={3}
          fullWidth
          value={rejectComment}
          onChange={(e) => setRejectComment(e.target.value)}
          sx={{ mt: 1 }}
        />
      </ConfirmDialog>

      {/* Revision Dialog */}
      <ConfirmDialog
        open={showRevisionDialog}
        title="Request Revision"
        message=""
        confirmLabel="Request Revision"
        onConfirm={() => revisionMutation.mutate()}
        onCancel={() => setShowRevisionDialog(false)}
        loading={revisionMutation.isPending}
      >
        <TextField
          label="Revision instructions"
          multiline
          rows={3}
          fullWidth
          value={revisionComment}
          onChange={(e) => setRevisionComment(e.target.value)}
          sx={{ mt: 1 }}
        />
      </ConfirmDialog>

      {/* Delete Dialog */}
      <ConfirmDialog
        open={showDeleteDialog}
        title="Delete Proposal"
        message={`Are you sure you want to permanently delete "${proposal.title}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setShowDeleteDialog(false)}
        loading={deleteMutation.isPending}
        danger
      />
    </Box>
  );
}
