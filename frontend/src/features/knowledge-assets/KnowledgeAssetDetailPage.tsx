import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { knowledgeAssetsApi } from '../../shared/api/knowledgeAssets';
import { peerReviewApi } from '../../shared/api/peer-review';
import { usersApi } from '../../shared/api/users';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { KnowledgeAssetTypeLabel } from '../../shared/types';
import type { AssetReviewDto, NominateReviewerRequest, SubmitReviewRequest } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';

const nominateSchema = z.object({
  reviewerId: z.string().min(1, 'Please select a reviewer'),
});

const submitReviewSchema = z.object({
  status: z.enum(['Approved', 'Rejected', 'RevisionRequested']),
  comments: z.string().max(1000).optional().or(z.literal('')),
});

const statusColor: Record<string, 'default' | 'warning' | 'success' | 'error' | 'info'> = {
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'error',
  RevisionRequested: 'info',
};

// F3: reject non-HTTP(S) URLs to prevent XSS via javascript: URIs
const safeHref = (url: string) => /^https?:\/\//i.test(url) ? url : '#';

export default function KnowledgeAssetDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { user } = useAuth();

  const [tab, setTab] = useState(0);
  const [showNominateDialog, setShowNominateDialog] = useState(false);
  const [submitReviewTarget, setSubmitReviewTarget] = useState<AssetReviewDto | null>(null);
  const toast = useToast();

  const { data: asset, isLoading, error } = useQuery({
    queryKey: ['knowledge-assets', id],
    queryFn: ({ signal }) => knowledgeAssetsApi.getAssetById(id!, signal),
    enabled: !!id,
  });

  const { data: reviews, isLoading: reviewsLoading } = useQuery({
    queryKey: ['knowledge-assets', id, 'reviews'],
    queryFn: ({ signal }) => peerReviewApi.getAssetReviews(id!, signal),
    enabled: !!id,
  });

  const { data: usersData } = useQuery({
    queryKey: ['users-for-nominate'],
    queryFn: ({ signal }) => usersApi.getUsers({ pageSize: 200 }, signal),
    enabled: showNominateDialog,
  });

  usePageTitle(asset?.title ?? 'Asset');

  const nominateForm = useForm<NominateReviewerRequest>({
    resolver: zodResolver(nominateSchema),
    defaultValues: { reviewerId: '' },
  });

  const submitReviewForm = useForm<SubmitReviewRequest>({
    resolver: zodResolver(submitReviewSchema),
    defaultValues: { status: 'Approved', comments: '' },
  });

  const nominateMutation = useMutation({
    mutationFn: (data: NominateReviewerRequest) => peerReviewApi.nominateReviewer(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-assets', id, 'reviews'] });
      setShowNominateDialog(false);
      nominateForm.reset();
      toast.success('Reviewer nominated.');
    },
    onError: () => toast.error('Failed to nominate reviewer.'),
  });

  const submitReviewMutation = useMutation({
    mutationFn: (data: SubmitReviewRequest) => peerReviewApi.submitReview(submitReviewTarget!.id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-assets', id, 'reviews'] });
      setSubmitReviewTarget(null);
      submitReviewForm.reset();
      toast.success('Review submitted.');
    },
    onError: () => toast.error('Failed to submit review.'),
  });

  const myPendingReview = reviews?.find(
    (r) => r.reviewerId === user?.userId && r.status === 'Pending',
  );

  if (isLoading) return <LoadingOverlay />;
  if (error || !asset) return <ApiErrorAlert error={error ?? new Error('Asset not found')} />;

  return (
    <Box>
      <PageHeader
        title={asset.title}
        actions={
          <Button
            variant="contained"
            component="a"
            href={safeHref(asset.url)}
            target="_blank"
            rel="noopener noreferrer"
          >
            Open Asset
          </Button>
        }
      />

      <Stack direction="row" spacing={1} mb={2} flexWrap="wrap">
        <Chip
          label={KnowledgeAssetTypeLabel[asset.assetType] ?? 'Unknown'}
          size="small"
          color="primary"
          variant="outlined"
        />
        {asset.isVerified && <Chip label="Verified" size="small" color="success" />}
        {!asset.isPublic && <Chip label="Private" size="small" color="warning" />}
      </Stack>

      <Stack direction="row" spacing={3} mb={3} flexWrap="wrap">
        <Typography variant="body2" color="text.secondary">
          <strong>{asset.viewCount}</strong> views
        </Typography>
        <Typography variant="body2" color="text.secondary">
          <strong>{asset.downloadCount}</strong> downloads
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Uploaded {new Date(asset.createdDate).toLocaleDateString()} by{' '}
          <strong>{asset.createdBy}</strong>
        </Typography>
      </Stack>

      <Paper>
        <Tabs value={tab} onChange={(_, v: number) => setTab(v)}>
          <Tab label="Overview" />
          <Tab label={`Peer Reviews (${reviews?.length ?? 0})`} />
        </Tabs>
        <Divider />

        {/* Overview Tab */}
        {tab === 0 && (
          <Box p={3}>
            {asset.description ? (
              <Typography variant="body1" whiteSpace="pre-wrap">
                {asset.description}
              </Typography>
            ) : (
              <Typography color="text.secondary">No description provided.</Typography>
            )}
            {asset.sessionId && (
              <Box mt={2}>
                <Typography variant="body2" color="text.secondary">
                  Linked session:{' '}
                  <Link to={`/sessions/${asset.sessionId}`} style={{ color: 'inherit' }}>
                    View session
                  </Link>
                </Typography>
              </Box>
            )}
          </Box>
        )}

        {/* Peer Reviews Tab */}
        {tab === 1 && (
          <Box p={3}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2} flexWrap="wrap" gap={1}>
              <Typography variant="h6">Reviews</Typography>
              <Stack direction="row" spacing={1}>
                {myPendingReview && (
                  <Button
                    variant="outlined"
                    onClick={() => {
                      setSubmitReviewTarget(myPendingReview);
                      submitReviewForm.reset({ status: 'Approved', comments: '' });
                    }}
                  >
                    Submit My Review
                  </Button>
                )}
                <Button variant="contained" onClick={() => setShowNominateDialog(true)}>
                  Nominate Reviewer
                </Button>
              </Stack>
            </Box>

            {reviewsLoading && <LoadingOverlay />}

            {!reviewsLoading && reviews?.length === 0 && (
              <Typography color="text.secondary">No peer reviews yet.</Typography>
            )}

            <Stack spacing={2}>
              {reviews?.map((review) => (
                <Paper key={review.id} variant="outlined" sx={{ p: 2 }}>
                  <Box
                    display="flex"
                    justifyContent="space-between"
                    alignItems="flex-start"
                    flexWrap="wrap"
                    gap={1}
                  >
                    <Box>
                      <Typography variant="subtitle2">{review.reviewerName}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        Nominated by {review.nominatedByUserName} on{' '}
                        {new Date(review.nominatedAt).toLocaleDateString()}
                      </Typography>
                    </Box>
                    <Chip
                      label={review.status}
                      size="small"
                      color={statusColor[review.status] ?? 'default'}
                    />
                  </Box>
                  {review.comments && (
                    <Typography variant="body2" mt={1}>
                      {review.comments}
                    </Typography>
                  )}
                  {review.reviewedAt && (
                    <Typography variant="caption" color="text.secondary" display="block" mt={0.5}>
                      Reviewed on {new Date(review.reviewedAt).toLocaleDateString()}
                    </Typography>
                  )}
                </Paper>
              ))}
            </Stack>
          </Box>
        )}
      </Paper>

      {/* Nominate Reviewer Dialog */}
      <Dialog
        open={showNominateDialog}
        onClose={() => setShowNominateDialog(false)}
        maxWidth="xs"
        fullWidth
      >
        <form onSubmit={nominateForm.handleSubmit((data) => nominateMutation.mutate(data))}>
          <DialogTitle>Nominate Reviewer</DialogTitle>
          <DialogContent>
            <Stack spacing={2} mt={1}>
              <ApiErrorAlert error={nominateMutation.error} />
              <Controller
                name="reviewerId"
                control={nominateForm.control}
                render={({ field, fieldState }) => (
                  <FormControl fullWidth error={!!fieldState.error}>
                    <InputLabel>Reviewer</InputLabel>
                    <Select {...field} label="Reviewer">
                      {usersData?.data
                        .filter((u) => u.id !== user?.userId)
                        .map((u) => (
                          <MenuItem key={u.id} value={u.id}>
                            {u.fullName}
                            {u.designation ? ` — ${u.designation}` : ''}
                          </MenuItem>
                        ))}
                    </Select>
                    {fieldState.error && (
                      <Typography variant="caption" color="error">
                        {fieldState.error.message}
                      </Typography>
                    )}
                  </FormControl>
                )}
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowNominateDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={nominateMutation.isPending}>
              Nominate
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Submit Review Dialog */}
      <Dialog
        open={!!submitReviewTarget}
        onClose={() => setSubmitReviewTarget(null)}
        maxWidth="sm"
        fullWidth
      >
        <form onSubmit={submitReviewForm.handleSubmit((data) => submitReviewMutation.mutate(data))}>
          <DialogTitle>Submit Peer Review</DialogTitle>
          <DialogContent>
            <Stack spacing={2} mt={1}>
              <ApiErrorAlert error={submitReviewMutation.error} />
              <Controller
                name="status"
                control={submitReviewForm.control}
                render={({ field }) => (
                  <FormControl fullWidth>
                    <InputLabel>Decision</InputLabel>
                    <Select {...field} label="Decision">
                      <MenuItem value="Approved">Approved</MenuItem>
                      <MenuItem value="Rejected">Rejected</MenuItem>
                      <MenuItem value="RevisionRequested">Revision Requested</MenuItem>
                    </Select>
                  </FormControl>
                )}
              />
              <TextField
                label="Comments (optional)"
                multiline
                rows={3}
                fullWidth
                {...submitReviewForm.register('comments')}
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setSubmitReviewTarget(null)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={submitReviewMutation.isPending}>
              Submit
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
}
