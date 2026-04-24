import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  LinearProgress,
  Stack,
  Tooltip,
  Typography,
} from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { screeningApi } from '../talentApi';
import { CreateScreeningDialog } from './components/CreateScreeningDialog';
import { ReScreenDialog } from './components/ReScreenDialog';
import { usePageTitle } from '../../../shared/hooks/usePageTitle';
import { PageHeader } from '../../../shared/components/PageHeader';
import { useToast } from '../../../shared/hooks/useToast';
import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import type { ScreeningJobStatus } from '../types';

function statusColor(status: ScreeningJobStatus): 'default' | 'info' | 'success' | 'error' | 'warning' {
  switch (status) {
    case 'Pending': return 'default';
    case 'Processing': return 'info';
    case 'Completed': return 'success';
    case 'Failed': return 'error';
    case 'Cancelled': return 'warning';
  }
}

export default function ScreeningListPage() {
  usePageTitle('Resume Screener');
  const navigate = useNavigate();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [reScreenJobId, setReScreenJobId] = useState<string | null>(null);
  const [deleteJobId, setDeleteJobId] = useState<string | null>(null);
  const [deleteJobTitle, setDeleteJobTitle] = useState('');

  // FE-04: only poll while at least one job is actively processing;
  // constant 10 s polling fires even when all jobs are idle
  const { data: jobs, isLoading, refetch } = useQuery({
    queryKey: ['screening', 'list'],
    queryFn: screeningApi.getJobs,
    refetchInterval: (query) =>
      query.state.data?.some((j) => j.status === 'Processing') ? 10_000 : false,
  });

  const deleteMutation = useMutation({
    mutationFn: (jobId: string) => screeningApi.deleteJob(jobId),
    onSuccess: () => {
      toast.success('Screening job deleted.');
      setDeleteJobId(null);
      void queryClient.invalidateQueries({ queryKey: ['screening', 'list'] });
    },
    onError: () => toast.error('Failed to delete screening job.'),
  });

  const handleJobCreated = (jobId: string) => {
    void refetch();
    navigate(`/talent/screening/${jobId}`);
  };

  const handleDeleteClick = (e: React.MouseEvent, jobId: string, jobTitle: string) => {
    e.stopPropagation();
    setDeleteJobTitle(jobTitle);
    setDeleteJobId(jobId);
  };

  return (
    <Box>
      <PageHeader
        title="Resume Screener"
        subtitle="AI-powered bulk resume screening"
        actions={
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setDialogOpen(true)}
          >
            New Screening
          </Button>
        }
      />

      {isLoading ? (
        <LinearProgress />
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Job Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Candidates</TableCell>
                <TableCell>Progress</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(jobs ?? []).length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <Typography color="text.secondary" py={3}>
                      No screening jobs yet. Click "New Screening" to get started.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {(jobs ?? []).map((job) => (
                <TableRow
                  key={job.id}
                  hover
                  sx={{ cursor: 'pointer' }}
                  onClick={() => navigate(`/talent/screening/${job.id}`)}
                >
                  <TableCell>
                    <Typography fontWeight={500}>{job.jobTitle}</Typography>
                  </TableCell>
                  <TableCell>
                    <Chip label={job.status} size="small" color={statusColor(job.status)} />
                  </TableCell>
                  <TableCell>{job.totalCandidates}</TableCell>
                  <TableCell sx={{ minWidth: 160 }}>
                    <Stack direction="row" alignItems="center" gap={1}>
                      <LinearProgress
                        variant="determinate"
                        value={job.progressPercent}
                        sx={{ flex: 1, height: 6, borderRadius: 3 }}
                      />
                      <Typography variant="caption">{job.progressPercent}%</Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {new Date(job.createdAt).toLocaleDateString()}
                    </Typography>
                  </TableCell>
                  <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                    <Stack direction="row" spacing={1} justifyContent="flex-end">
                      {(job.status === 'Completed' || job.status === 'Failed') && (
                        <Button
                          size="small"
                          variant="outlined"
                          onClick={() => setReScreenJobId(job.id)}
                        >
                          Re-screen
                        </Button>
                      )}
                      <Tooltip title={job.status === 'Processing' ? 'Cancel job' : 'Delete job'}>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={(e) => handleDeleteClick(e, job.id, job.jobTitle)}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <CreateScreeningDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        onJobCreated={handleJobCreated}
      />

      {reScreenJobId && (
        <ReScreenDialog
          open
          onClose={() => setReScreenJobId(null)}
          jobId={reScreenJobId}
        />
      )}

      {/* Delete confirmation dialog */}
      <Dialog open={!!deleteJobId} onClose={() => setDeleteJobId(null)} maxWidth="xs" fullWidth>
        <DialogTitle>
          {jobs?.find((j) => j.id === deleteJobId)?.status === 'Processing'
            ? 'Cancel Screening Job?'
            : 'Delete Screening Job?'}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            {jobs?.find((j) => j.id === deleteJobId)?.status === 'Processing'
              ? `"${deleteJobTitle}" is currently processing. It will be cancelled and kept in your history.`
              : `Are you sure you want to permanently delete "${deleteJobTitle}"? This cannot be undone.`}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteJobId(null)} disabled={deleteMutation.isPending}>
            Keep
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={deleteMutation.isPending}
            onClick={() => deleteJobId && deleteMutation.mutate(deleteJobId)}
          >
            {deleteMutation.isPending
              ? 'Deleting…'
              : jobs?.find((j) => j.id === deleteJobId)?.status === 'Processing'
                ? 'Cancel Job'
                : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
