import {
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
  Tab,
  Tabs,
  TextField,
  Typography,
  FlagIcon,
} from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { moderationApi } from '../../shared/api/moderation';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { UserAutocomplete } from '../../shared/components/UserAutocomplete';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';
import type { ContentFlagDto, FlagStatus, ReviewFlagRequest, UserDto } from '../../shared/types';

// ── Helpers ──────────────────────────────────────────────────────────────────
function statusColor(s: FlagStatus) {
  return s === 'ActionTaken' ? 'error'
    : s === 'Reviewed' ? 'success'
      : s === 'Dismissed' ? 'default'
        : 'warning';
}

// ── Review flag dialog ────────────────────────────────────────────────────────
interface ReviewDialogProps {
  flag: ContentFlagDto;
  onClose: () => void;
}

function ReviewFlagDialog({ flag, onClose }: ReviewDialogProps) {
  const qc = useQueryClient();
  const toast = useToast();
  const { register, handleSubmit, control } = useForm<ReviewFlagRequest>({
    defaultValues: { status: 'Reviewed', reviewNotes: '' },
  });

  const mutation = useMutation({
    mutationFn: (data: ReviewFlagRequest) => moderationApi.reviewFlag(flag.id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['moderation', 'flags'] }); onClose(); toast.success('Flag reviewed.'); },
    onError: () => toast.error('Failed to review flag.'),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Review Flag</DialogTitle>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <DialogContent>
          <Stack spacing={2}>
            <Box>
              <Typography variant="body2" color="text.secondary">Content type</Typography>
              <Typography>{flag.contentType} — {flag.contentId}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">Reason</Typography>
              <Typography>{flag.reason}</Typography>
            </Box>
            {flag.notes && (
              <Box>
                <Typography variant="body2" color="text.secondary">Notes from flagger</Typography>
                <Typography>{flag.notes}</Typography>
              </Box>
            )}
            <Controller
              name="status"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth>
                  <InputLabel>Decision</InputLabel>
                  <Select {...field} label="Decision">
                    <MenuItem value="Reviewed">Reviewed (no action)</MenuItem>
                    <MenuItem value="Dismissed">Dismissed</MenuItem>
                    <MenuItem value="ActionTaken">Action Taken</MenuItem>
                  </Select>
                </FormControl>
              )}
            />
            <TextField
              {...register('reviewNotes')}
              label="Review Notes"
              multiline
              rows={3}
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>Submit</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── Flags tab ────────────────────────────────────────────────────────────────
function ContentFlagsTab() {
  const [statusFilter, setStatusFilter] = useState<FlagStatus | ''>('Pending');
  const [reviewFlag, setReviewFlag] = useState<ContentFlagDto | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['moderation', 'flags', statusFilter],
    queryFn: ({ signal }) => moderationApi.getContentFlags({ status: statusFilter || undefined, pageSize: 50 }, signal),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" spacing={2} mb={3} alignItems="center">
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Status</InputLabel>
          <Select value={statusFilter} label="Status" onChange={(e) => setStatusFilter(e.target.value as FlagStatus | '')}>
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Pending">Pending</MenuItem>
            <MenuItem value="Reviewed">Reviewed</MenuItem>
            <MenuItem value="Dismissed">Dismissed</MenuItem>
            <MenuItem value="ActionTaken">Action Taken</MenuItem>
          </Select>
        </FormControl>
        <Typography variant="body2" color="text.secondary">
          {data?.totalCount ?? 0} flags
        </Typography>
      </Stack>

      <Stack spacing={1.5}>
        {data?.data.map((flag) => (
          <Card key={flag.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box flex={1}>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <Chip label={flag.contentType} size="small" />
                    <Chip label={flag.reason} size="small" color="warning" variant="outlined" />
                    <Chip label={flag.status} size="small" color={statusColor(flag.status)} />
                  </Stack>
                  <Typography variant="body2">Content ID: {flag.contentId}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Flagged by {flag.flaggedByUserName} · {new Date(flag.createdDate).toLocaleDateString()}
                  </Typography>
                  {flag.notes && (
                    <Typography variant="caption" display="block" sx={{ mt: 0.5, fontStyle: 'italic' }}>
                      "{flag.notes}"
                    </Typography>
                  )}
                </Box>
                {flag.status === 'Pending' && (
                  <Button size="small" startIcon={<FlagIcon />} onClick={() => setReviewFlag(flag)}>
                    Review
                  </Button>
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
        {data?.data.length === 0 && (
          <Typography color="text.secondary">No flags found.</Typography>
        )}
      </Stack>

      {reviewFlag && (
        <ReviewFlagDialog flag={reviewFlag} onClose={() => setReviewFlag(null)} />
      )}
    </Box>
  );
}

// ── Suspend dialog ────────────────────────────────────────────────────────────
function SuspendUserDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const { register, handleSubmit, reset } = useForm<{ reason: string; expiresAt: string }>({
    defaultValues: { reason: '', expiresAt: '' },
  });

  const mutation = useMutation({
    mutationFn: (d: { reason: string; expiresAt: string }) =>
      moderationApi.suspendUser({ userId: selectedUser!.id, reason: d.reason, expiresAt: d.expiresAt || undefined }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['moderation', 'suspensions'] }); reset(); setSelectedUser(null); onClose(); toast.success('User suspended.'); },
    onError: () => toast.error('Failed to suspend user.'),
  });

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Suspend User</DialogTitle>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <UserAutocomplete
              value={selectedUser}
              onChange={setSelectedUser}
              label="User"
              required
              fullWidth
            />
            <TextField {...register('reason', { required: true })} label="Reason" multiline rows={3} fullWidth required />
            <TextField {...register('expiresAt')} label="Expires At (optional)" type="datetime-local" fullWidth
              InputLabelProps={{ shrink: true }} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" color="error" disabled={mutation.isPending || !selectedUser}>Suspend</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── Suspensions tab ──────────────────────────────────────────────────────────
function UserSuspensionsTab() {
  const [suspendOpen, setSuspendOpen] = useState(false);
  // F13: require an explicit reason instead of hardcoding 'Manually lifted'
  const [liftDialogOpen, setLiftDialogOpen] = useState(false);
  const [liftTargetId, setLiftTargetId] = useState<string | null>(null);
  const [liftReasonText, setLiftReasonText] = useState('');
  const qc = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['moderation', 'suspensions'],
    queryFn: ({ signal }) => moderationApi.getActiveSuspensions({ pageSize: 50 }, signal),
  });

  const toast = useToast();
  const liftMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      moderationApi.liftSuspension(id, { liftReason: reason }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['moderation', 'suspensions'] }); toast.success('Suspension lifted.'); },
    onError: () => toast.error('Failed to lift suspension.'),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" justifyContent="flex-end" mb={2}>
        <Button variant="contained" color="error" onClick={() => setSuspendOpen(true)}>
          Suspend User
        </Button>
      </Stack>
      <Stack spacing={1.5}>
        {data?.data.map((s) => (
          <Card key={s.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <Typography fontWeight={600}>{s.userName}</Typography>
                    <Chip label={s.isActive ? 'Active' : 'Lifted'} size="small"
                      color={s.isActive ? 'error' : 'default'} />
                  </Stack>
                  <Typography variant="body2">{s.reason}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Suspended {new Date(s.suspendedAt).toLocaleDateString()}
                    {s.expiresAt && ` · Expires ${new Date(s.expiresAt).toLocaleDateString()}`}
                  </Typography>
                </Box>
                {s.isActive && (
                  <Button size="small" color="success"
                    onClick={() => { setLiftTargetId(s.id); setLiftDialogOpen(true); }}>
                    Lift
                  </Button>
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
        {data?.data.length === 0 && (
          <Typography color="text.secondary">No active suspensions.</Typography>
        )}
      </Stack>
      <SuspendUserDialog open={suspendOpen} onClose={() => setSuspendOpen(false)} />

      {/* F13: prompt for a reason before lifting a suspension */}
      <Dialog open={liftDialogOpen} onClose={() => setLiftDialogOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Lift Suspension</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="Reason for lifting"
            value={liftReasonText}
            onChange={(e) => setLiftReasonText(e.target.value)}
            autoFocus
            margin="dense"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setLiftDialogOpen(false); setLiftReasonText(''); }}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!liftReasonText.trim() || liftMutation.isPending}
            onClick={() => {
              if (liftTargetId) {
                liftMutation.mutate({ id: liftTargetId, reason: liftReasonText.trim() });
              }
              setLiftDialogOpen(false);
              setLiftReasonText('');
              setLiftTargetId(null);
            }}
          >
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

// ── Main page ────────────────────────────────────────────────────────────────
export default function ModerationPage() {
  usePageTitle('Moderation');
  const [tab, setTab] = useState(0);

  return (
    <Box>
      <PageHeader title="Moderation" subtitle="Review flagged content and manage user suspensions" />
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tab} onChange={(_, v) => setTab(v)}>
          <Tab label="Content Flags" />
          <Tab label="User Suspensions" />
        </Tabs>
      </Box>
      {tab === 0 && <ContentFlagsTab />}
      {tab === 1 && <UserSuspensionsTab />}
    </Box>
  );
}
