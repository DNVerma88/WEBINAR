import { useState } from 'react';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { ResendDialog } from '../components/ResendDialog';
import { surveysApi } from '../api/surveysApi';
import type { SurveyInvitationDto, SurveyInvitationStatus } from '../types';

const STATUS_FILTERS: { value: SurveyInvitationStatus | 'All'; label: string }[] = [
  { value: 'All',       label: 'All' },
  { value: 'Pending',   label: 'Pending' },
  { value: 'Sent',      label: 'Sent' },
  { value: 'Submitted', label: 'Submitted' },
  { value: 'Expired',   label: 'Expired' },
  { value: 'Failed',    label: 'Failed' },
];

const STATUS_COLORS: Record<SurveyInvitationStatus, 'default' | 'info' | 'success' | 'warning' | 'error'> = {
  Pending:   'default',
  Sent:      'info',
  Submitted: 'success',
  Expired:   'warning',
  Failed:    'error',
};

interface InvitationsTabProps {
  surveyId: string;
}

export function InvitationsTab({ surveyId }: InvitationsTabProps) {
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<SurveyInvitationStatus | 'All'>('All');
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [resendOpen, setResendOpen] = useState(false);
  const [resendMode, setResendMode] = useState<'single' | 'bulk' | 'expired'>('bulk');
  const [singleUserId, setSingleUserId] = useState<string>('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['surveys', surveyId, 'invitations', statusFilter],
    queryFn: () =>
      surveysApi.getInvitations(surveyId, {
        pageNumber: 1,
        pageSize: 200,
        status: statusFilter === 'All' ? undefined : statusFilter,
      }),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['surveys', surveyId, 'invitations'] });

  const resendOneMut = useMutation({
    mutationFn: (userId: string) => surveysApi.resendToUser(surveyId, userId),
    onSuccess: () => { setResendOpen(false); invalidate(); },
  });

  const resendBulkMut = useMutation({
    mutationFn: (userIds: string[]) => surveysApi.resendBulk(surveyId, { userIds }),
    onSuccess: () => { setResendOpen(false); setSelectedIds(new Set()); invalidate(); },
  });

  const resendAllMut = useMutation({
    mutationFn: () => surveysApi.resendAllPending(surveyId),
    onSuccess: () => { setResendOpen(false); invalidate(); },
  });

  const invitations: SurveyInvitationDto[] = data?.data ?? [];

  function toggleSelected(inv: SurveyInvitationDto) {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(inv.userId)) next.delete(inv.userId);
      else next.add(inv.userId);
      return next;
    });
  }

  function openResendSingle(userId: string) {
    setSingleUserId(userId);
    setResendMode('single');
    setResendOpen(true);
  }

  function openResendBulk() {
    setResendMode('bulk');
    setResendOpen(true);
  }

  function openResendExpired() {
    setResendMode('expired');
    setResendOpen(true);
  }

  function handleResendConfirm() {
    if (resendMode === 'single') resendOneMut.mutate(singleUserId);
    else if (resendMode === 'bulk') resendBulkMut.mutate([...selectedIds]);
    else resendAllMut.mutate();
  }

  const resendLoading = resendOneMut.isPending || resendBulkMut.isPending || resendAllMut.isPending;

  const resendNames = resendMode === 'single'
    ? invitations.filter(i => i.userId === singleUserId).map(i => i.userFullName)
    : resendMode === 'bulk'
    ? invitations.filter(i => selectedIds.has(i.userId)).map(i => i.userFullName)
    : invitations.filter(i => i.status === 'Expired').map(i => i.userFullName);

  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      {/* Toolbar */}
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2, flexWrap: 'wrap' }}>
        {STATUS_FILTERS.map(f => (
          <Chip
            key={f.value}
            label={f.label}
            onClick={() => setStatusFilter(f.value)}
            color={statusFilter === f.value ? 'primary' : 'default'}
            variant={statusFilter === f.value ? 'filled' : 'outlined'}
            size="small"
          />
        ))}
        <Box sx={{ flex: 1 }} />
        {selectedIds.size > 0 && (
          <Button
            size="small"
            startIcon={<RefreshIcon />}
            variant="outlined"
            onClick={openResendBulk}
          >
            Resend Selected ({selectedIds.size})
          </Button>
        )}
        <Button
          size="small"
          variant="outlined"
          color="warning"
          onClick={openResendExpired}
        >
          Resend All Expired
        </Button>
      </Stack>

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox" />
              <TableCell>Full Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Sent</TableCell>
              <TableCell>Expires</TableCell>
              <TableCell>Resend Count</TableCell>
              <TableCell>Action</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : invitations.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  <Typography variant="body2" color="text.secondary">No invitations found.</Typography>
                </TableCell>
              </TableRow>
            ) : (
              invitations.map(inv => (
                <TableRow key={inv.id} hover selected={selectedIds.has(inv.userId)}>
                  <TableCell padding="checkbox">
                    <input
                      type="checkbox"
                      checked={selectedIds.has(inv.userId)}
                      onChange={() => toggleSelected(inv)}
                      disabled={inv.status === 'Submitted'}
                    />
                  </TableCell>
                  <TableCell>{inv.userFullName}</TableCell>
                  <TableCell>{inv.userEmail}</TableCell>
                  <TableCell>
                    <Chip
                      label={inv.status}
                      size="small"
                      color={STATUS_COLORS[inv.status]}
                    />
                  </TableCell>
                  <TableCell>
                    {inv.sentAt ? new Date(inv.sentAt).toLocaleDateString() : '—'}
                  </TableCell>
                  <TableCell>
                    {inv.expiresAt ? new Date(inv.expiresAt).toLocaleDateString() : '—'}
                  </TableCell>
                  <TableCell align="center">{inv.resendCount}</TableCell>
                  <TableCell>
                    <Tooltip title={inv.status === 'Submitted' ? 'Already submitted' : 'Resend invitation'}>
                      <span>
                        <IconButton
                          size="small"
                          disabled={inv.status === 'Submitted'}
                          onClick={() => openResendSingle(inv.userId)}
                        >
                          <RefreshIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <ResendDialog
        open={resendOpen}
        userNames={resendNames}
        onConfirm={handleResendConfirm}
        onCancel={() => setResendOpen(false)}
        loading={resendLoading}
      />
    </Box>
  );
}
