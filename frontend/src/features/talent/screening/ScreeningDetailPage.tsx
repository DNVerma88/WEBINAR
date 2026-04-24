import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  LinearProgress,
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { DownloadIcon, DeleteIcon, CloseIcon } from '@/components/ui';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { screeningApi } from '../talentApi';
import { BulkProgressPanel } from './components/BulkProgressPanel';
import { CandidateResultCard } from './components/CandidateResultCard';
import { CompareDialog } from './components/CompareDialog';
import { ReScreenDialog } from './components/ReScreenDialog';
import { ResumeSourcePanel } from './components/ResumeSourcePanel';
import { usePageTitle } from '../../../shared/hooks/usePageTitle';
import { PageHeader } from '../../../shared/components/PageHeader';
import { useToast } from '../../../shared/hooks/useToast';
import type { ScreeningJobStatus, ScreeningCandidate } from '../types';
import { useState } from 'react';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  IconButton,
  Paper,
} from '@mui/material';
import { ExpandMore as ExpandMoreIcon } from '@mui/icons-material';

function statusColor(status: ScreeningJobStatus): 'default' | 'info' | 'success' | 'error' | 'warning' {
  switch (status) {
    case 'Pending': return 'default';
    case 'Processing': return 'info';
    case 'Completed': return 'success';
    case 'Failed': return 'error';
    case 'Cancelled': return 'warning';
  }
}

function downloadBlob(data: Blob, filename: string) {
  const url = URL.createObjectURL(data);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export default function ScreeningDetailPage() {
  usePageTitle('Screening Details');
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [selectedCandidate, setSelectedCandidate] = useState<ScreeningCandidate | null>(null);
  const [reScreenOpen, setReScreenOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [editJdOpen, setEditJdOpen] = useState(false);
  const [editJdText, setEditJdText] = useState('');
  const [jdSaving, setJdSaving] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [compareOpen, setCompareOpen] = useState(false);

  const { data: job, isLoading, refetch } = useQuery({
    queryKey: ['screening', jobId],
    queryFn: () => screeningApi.getJob(jobId!),
    enabled: !!jobId,
    refetchInterval: (q) => q.state.data?.status === 'Processing' ? 5_000 : false,
  });

  const deleteMutation = useMutation({
    mutationFn: () => screeningApi.deleteJob(jobId!),
    onSuccess: () => {
      toast.success('Screening job deleted.');
      void queryClient.invalidateQueries({ queryKey: ['screening', 'list'] });
      navigate('/talent/screening');
    },
    onError: () => toast.error('Failed to delete screening job.'),
  });

  const handleExportCsv = async () => {
    if (!jobId) return;
    const blob = await screeningApi.exportCsv(jobId);
    downloadBlob(blob, `screening-${jobId}.csv`);
  };

  const handleOpenEditJd = () => {
    setEditJdText(job?.jdText ?? '');
    setEditJdOpen(true);
  };

  const handleSaveJd = async () => {
    const trimmed = editJdText.trim();
    if (!trimmed) return;
    setJdSaving(true);
    try {
      await screeningApi.updateJd(jobId!, trimmed);
      toast.success('Job description updated.');
      void refetch();
      setEditJdOpen(false);
    } catch {
      toast.error('Failed to update job description.');
    } finally {
      setJdSaving(false);
    }
  };

  const handleSelectCandidate = (id: string, checked: boolean) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) {
        // Max 2 selections
        if (next.size < 2) next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  };

  if (isLoading) return <LinearProgress />;
  if (!job) return <Typography color="error">Job not found.</Typography>;

  const sortedCandidates = [...(job.candidates ?? [])].sort(
    (a, b) => (b.overallScore ?? 0) - (a.overallScore ?? 0),
  );

  return (
    <Box>
      <PageHeader
        title={job.jobTitle}
        subtitle={`Screening job · Created ${new Date(job.createdAt).toLocaleDateString()}`}
        breadcrumbs={[
          { label: 'Screener', to: '/talent/screening' },
          { label: job.jobTitle },
        ]}
        actions={
          <Stack direction="row" spacing={1}>
          {(job.status === 'Completed' || job.status === 'Failed') && (
              <Button variant="outlined" onClick={() => setReScreenOpen(true)}>
                Re-screen
              </Button>
            )}
            {job.status !== 'Processing' && (
              <Button variant="outlined" onClick={handleOpenEditJd}>
                Edit JD
              </Button>
            )}
            {job.status === 'Completed' && (
              <Button
                variant="outlined"
                startIcon={<DownloadIcon />}
                onClick={() => void handleExportCsv()}
              >
                Export CSV
              </Button>
            )}
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteIcon />}
              disabled={deleteMutation.isPending}
              onClick={() => setDeleteConfirmOpen(true)}
            >
              {job.status === 'Processing' ? 'Cancel' : 'Delete'}
            </Button>
            <Button variant="outlined" onClick={() => navigate('/talent/screening')}>
              Back
            </Button>
          </Stack>
        }
      />

      {/* Status banner */}
      <Box display="flex" alignItems="center" gap={2} mb={3}>
        <Chip label={job.status} color={statusColor(job.status)} />
        <Typography variant="body2" color="text.secondary">
          {job.processedCandidates} / {job.totalCandidates} candidates processed
        </Typography>
      </Box>

      {job.errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {job.errorMessage}
        </Alert>
      )}

      {/* Live progress panel */}
      {job.status === 'Processing' && (
        <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
          <BulkProgressPanel jobId={job.id} totalCandidates={job.totalCandidates} />
        </Paper>
      )}

      {/* Results — scored candidates */}
      {job.status === 'Completed' && (
        <Box>
          <Box display="flex" alignItems="center" gap={2} mb={1}>
            <Typography variant="h6">
              Results ({sortedCandidates.length})
            </Typography>
            {selectedIds.size === 2 && (
              <Button
                variant="contained"
                size="small"
                onClick={() => setCompareOpen(true)}
              >
                Compare Selected (2)
              </Button>
            )}
            {selectedIds.size === 1 && (
              <Typography variant="body2" color="text.secondary">
                Select one more candidate to compare
              </Typography>
            )}
          </Box>
          {sortedCandidates.map((c) => (
            <CandidateResultCard
              key={c.id}
              candidate={c}
              onViewDetail={c.status === 'Scored' ? setSelectedCandidate : undefined}
              selected={selectedIds.has(c.id)}
              onSelect={handleSelectCandidate}
            />
          ))}
        </Box>
      )}

      {/* Add more resumes — available whenever screening is not actively running */}
      {job.status !== 'Processing' && job.status !== 'Cancelled' && (
        <Accordion sx={{ mt: 3 }}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle1" fontWeight={600}>
              Add More Resumes
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <ResumeSourcePanel
              jobId={job.id}
              onFilesAdded={() => void refetch()}
            />
          </AccordionDetails>
        </Accordion>
      )}

      <ReScreenDialog
        open={reScreenOpen}
        onClose={() => setReScreenOpen(false)}
        jobId={job.id}
        currentJdText={job.jdText}
        currentPromptTemplate={job.promptTemplate}
      />

      {/* Delete confirmation dialog */}
      <Dialog open={deleteConfirmOpen} onClose={() => setDeleteConfirmOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>
          {job.status === 'Processing' ? 'Cancel Screening Job?' : 'Delete Screening Job?'}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            {job.status === 'Processing'
              ? `"${job.jobTitle}" is currently processing. It will be cancelled and kept in your history.`
              : `Are you sure you want to permanently delete "${job.jobTitle}"? This cannot be undone.`}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)} disabled={deleteMutation.isPending}>
            Keep
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={deleteMutation.isPending}
            onClick={() => deleteMutation.mutate()}
          >
            {deleteMutation.isPending
              ? 'Deleting…'
              : job.status === 'Processing'
                ? 'Cancel Job'
                : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Standalone Edit JD dialog — for updating JD without re-screening */}
      <Dialog open={editJdOpen} onClose={() => !jdSaving && setEditJdOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Edit Job Description</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Update the job description. The new text will be used for the next screening or re-screening run.
          </Typography>
          <TextField
            multiline
            rows={12}
            fullWidth
            value={editJdText}
            onChange={(e) => setEditJdText(e.target.value)}
            placeholder="Paste or edit the job description here..."
            disabled={jdSaving}
            inputProps={{ maxLength: 100_000 }}
            helperText={`${editJdText.length.toLocaleString()} / 100,000 characters`}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditJdOpen(false)} disabled={jdSaving}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => void handleSaveJd()}
            disabled={jdSaving || !editJdText.trim()}
          >
            {jdSaving ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Candidate detail dialog */}
      {selectedCandidate && (
        <Dialog
          open
          onClose={() => setSelectedCandidate(null)}
          maxWidth="sm"
          fullWidth
        >
          <DialogTitle>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="h6">
                {selectedCandidate.candidateName ?? selectedCandidate.fileName}
              </Typography>
              <IconButton size="small" onClick={() => setSelectedCandidate(null)}>
                <CloseIcon fontSize="small" />
              </IconButton>
            </Box>
          </DialogTitle>
          <DialogContent>
            <CandidateResultCard candidate={selectedCandidate} />
          </DialogContent>
        </Dialog>
      )}

      {/* Compare two candidates side-by-side */}
      {compareOpen && selectedIds.size === 2 && (() => {
        const [idA, idB] = [...selectedIds];
        const candA = sortedCandidates.find((c) => c.id === idA);
        const candB = sortedCandidates.find((c) => c.id === idB);
        if (!candA || !candB) return null;
        return (
          <CompareDialog
            open
            onClose={() => setCompareOpen(false)}
            candidateA={candA}
            candidateB={candB}
          />
        );
      })()}
    </Box>
  );
}
