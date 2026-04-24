import {
  AddIcon,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  StarIcon,
  TextField,
  Typography,
  UpvoteIcon,
} from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { knowledgeRequestsApi } from '../../shared/api/knowledgeRequests';
import { sessionsApi } from '../../shared/api/sessions';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { KnowledgeRequestStatus, UserRole, KNOWLEDGE_REQUEST_STATUS_LABELS } from '../../shared/types';
import type { KnowledgeRequestDto } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const requestSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(1000).optional().or(z.literal('')),
  bountyXp: z.string().optional(),
});

type RequestFormValues = {
  title: string;
  description?: string;
  bountyXp?: string;
};

export default function KnowledgeRequestsPage() {
  usePageTitle('Knowledge Requests');
  const qc = useQueryClient();
  const { hasRole, user } = useAuth();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<KnowledgeRequestStatus | ''>('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [addressTarget, setAddressTarget] = useState<KnowledgeRequestDto | null>(null);
  const [addressSessionId, setAddressSessionId] = useState('');
  const toast = useToast();

  const { data, isLoading, error } = useQuery({
    queryKey: ['knowledge-requests', search, status, page],
    queryFn: ({ signal }) =>
      knowledgeRequestsApi.getKnowledgeRequests({
        search: search || undefined,
        status: status !== '' ? status : undefined,
        page,
        pageSize: 15,
      }, signal),
  });

  const upvoteMutation = useMutation({
    mutationFn: knowledgeRequestsApi.upvoteKnowledgeRequest,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['knowledge-requests'] }); toast.success('Upvoted!'); },
    onError: () => toast.error('Failed to upvote.'),
  });

  const claimMutation = useMutation({
    mutationFn: knowledgeRequestsApi.claimKnowledgeRequest,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['knowledge-requests'] }); toast.success('Request claimed!'); },
    onError: () => toast.error('Failed to claim request.'),
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => knowledgeRequestsApi.closeKnowledgeRequest(id, {}),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['knowledge-requests'] }); toast.success('Request closed.'); },
    onError: () => toast.error('Failed to close request.'),
  });

  const addressMutation = useMutation({
    mutationFn: ({ id, sessionId }: { id: string; sessionId: string }) =>
      knowledgeRequestsApi.addressKnowledgeRequest(id, { sessionId }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-requests'] });
      setAddressTarget(null);
      setAddressSessionId('');
      toast.success('Request marked as addressed.');
    },
    onError: () => toast.error('Failed to address request.'),
  });

  const { data: sessionsList } = useQuery({
    queryKey: ['sessions-for-kr'],
    queryFn: ({ signal }) => sessionsApi.getSessions({ pageSize: 200 }, signal),
    enabled: !!addressTarget,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RequestFormValues>({ resolver: zodResolver(requestSchema) });

  const createMutation = useMutation({
    mutationFn: knowledgeRequestsApi.createKnowledgeRequest,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-requests'] });
      setShowCreate(false);
      reset();
      toast.success('Knowledge request created!');
    },
    onError: () => toast.error('Failed to create request.'),
  });

  const onSubmit = (data: RequestFormValues) => {
    createMutation.mutate({
      title: data.title,
      description: data.description || undefined,
      bountyXp: data.bountyXp ? (parseInt(data.bountyXp, 10) || undefined) : undefined,
    });
  };

  const isAdmin = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);
  const isKT = hasRole(UserRole.KnowledgeTeam);
  const canClaim = isAdmin || hasRole(UserRole.Contributor) || isKT;
  const canAddress = isAdmin || isKT;

  const statusColor: Record<KnowledgeRequestStatus, 'default' | 'primary' | 'success' | 'error'> = {
    [KnowledgeRequestStatus.Open]: 'primary',
    [KnowledgeRequestStatus.InProgress]: 'default',
    [KnowledgeRequestStatus.Addressed]: 'success',
    [KnowledgeRequestStatus.Closed]: 'error',
  };

  return (
    <Box>
      <PageHeader
        title="Knowledge Requests"
        subtitle="Request topics you'd like to see covered"
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowCreate(true)}>
            New Request
          </Button>
        }
      />

      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 280 }}
        />
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => { setStatus(e.target.value as KnowledgeRequestStatus | ''); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {Object.values(KnowledgeRequestStatus).map((v) => (
              <MenuItem key={v} value={v}>{KNOWLEDGE_REQUEST_STATUS_LABELS[v]}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={upvoteMutation.error} />
      <ApiErrorAlert error={claimMutation.error} />
      <ApiErrorAlert error={closeMutation.error} />

      {isLoading ? (
        <LoadingOverlay />
      ) : (
        <>
          <Stack spacing={2}>
            {data?.data.map((req) => (
              <Card key={req.id}>
                <CardContent>
                  <Box display="flex" justifyContent="space-between" alignItems="flex-start">
                    <Box flex={1}>
                      <Box display="flex" alignItems="center" gap={1} mb={0.5} flexWrap="wrap">
                        <Typography variant="subtitle1" fontWeight={600}>
                          {req.title}
                        </Typography>
                        <Chip
                          label={KNOWLEDGE_REQUEST_STATUS_LABELS[req.status as KnowledgeRequestStatus] ?? req.status}
                          color={statusColor[req.status]}
                          size="small"
                        />
                        {req.bountyXp > 0 && (
                          <Chip
                            icon={<StarIcon />}
                            label={`${req.bountyXp} XP Bounty`}
                            size="small"
                            color="secondary"
                            variant="outlined"
                          />
                        )}
                      </Box>
                      {req.description && (
                        <Typography variant="body2" color="text.secondary" mb={0.5}>
                          {req.description}
                        </Typography>
                      )}
                      <Typography variant="caption" color="text.secondary">
                        Requested by {req.requesterName} · {new Date(req.createdDate).toLocaleDateString()}
                        {req.categoryName && ` · ${req.categoryName}`}
                        {req.claimedByName && ` · Claimed by ${req.claimedByName}`}
                      </Typography>
                    </Box>
                    <Stack direction="row" spacing={1} alignItems="center" ml={2}>
                      <Button
                        size="small"
                        startIcon={<UpvoteIcon />}
                        variant={req.hasUpvoted ? 'contained' : 'outlined'}
                        color="primary"
                        onClick={() => upvoteMutation.mutate(req.id)}
                        disabled={upvoteMutation.isPending || req.status === KnowledgeRequestStatus.Closed}
                      >
                        {req.upvoteCount}
                      </Button>
                      {canClaim && req.status === KnowledgeRequestStatus.Open && (
                        <Button
                          size="small"
                          variant="outlined"
                          color="success"
                          onClick={() => claimMutation.mutate(req.id)}
                          disabled={claimMutation.isPending}
                        >
                          Claim
                        </Button>
                      )}
                      {canAddress && (req.status === KnowledgeRequestStatus.Open || req.status === KnowledgeRequestStatus.InProgress) && (
                        <Button
                          size="small"
                          variant="outlined"
                          color="info"
                          onClick={() => { setAddressTarget(req); setAddressSessionId(''); }}
                        >
                          Mark Addressed
                        </Button>
                      )}
                      {(req.status !== KnowledgeRequestStatus.Closed) &&
                        (isAdmin || isKT || req.requesterId === user?.userId || req.claimedByUserId === user?.userId) && (
                        <Button
                          size="small"
                          variant="outlined"
                          color="error"
                          onClick={() => closeMutation.mutate(req.id)}
                          disabled={closeMutation.isPending}
                        >
                          Close
                        </Button>
                      )}
                    </Stack>
                  </Box>
                </CardContent>
              </Card>
            ))}
          </Stack>

          {data && data.data.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={6}>
              No knowledge requests found.
            </Typography>
          )}

          {data && data.totalPages > 1 && (
            <Box display="flex" justifyContent="center" mt={4} gap={1}>
              <Button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <Chip label={`${page} / ${data.totalPages}`} />
              <Button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </Box>
          )}
        </>
      )}

      {/* Mark Addressed Dialog */}
      <Dialog open={!!addressTarget} onClose={() => setAddressTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Mark as Addressed</DialogTitle>
        <DialogContent>
          <ApiErrorAlert error={addressMutation.error} />
          <Typography variant="body2" color="text.secondary" mb={2}>
            Select the session that directly addresses "{addressTarget?.title}".
          </Typography>
          <FormControl fullWidth>
            <InputLabel>Session</InputLabel>
            <Select
              value={addressSessionId}
              label="Session"
              onChange={(e) => setAddressSessionId(e.target.value)}
            >
              {(sessionsList?.data ?? []).map((s) => (
                <MenuItem key={s.id} value={s.id}>{s.title}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddressTarget(null)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!addressSessionId || addressMutation.isPending}
            onClick={() => addressTarget && addressMutation.mutate({ id: addressTarget.id, sessionId: addressSessionId })}
          >
            Confirm
          </Button>
        </DialogActions>
      </Dialog>

      {/* Create Dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogTitle>New Knowledge Request</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={createMutation.error} />
            <TextField
              {...register('title')}
              label="Topic you'd like covered"
              fullWidth
              autoFocus
              error={!!errors.title}
              helperText={errors.title?.message}
              sx={{ mt: 1, mb: 2 }}
            />
            <TextField
              {...register('description')}
              label="Additional context (optional)"
              multiline
              rows={3}
              fullWidth
              error={!!errors.description}
              helperText={errors.description?.message as string}
              sx={{ mb: 2 }}
            />
            <TextField
              {...register('bountyXp')}
              label="Bounty XP (optional)"
              type="number"
              fullWidth
              helperText="Offer XP reward for whoever fulfils this request"
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowCreate(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting || createMutation.isPending}>
              Submit Request
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
}
