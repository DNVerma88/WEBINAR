import { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Fab,
  FormControlLabel,
  IconButton,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import StopIcon from '@mui/icons-material/Stop';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { PageHeader } from '../../shared/components/PageHeader';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { surveysApi } from './api/surveysApi';
import { SurveyStatusChip } from './components/SurveyStatusChip';
import { CopySurveyDialog } from './components/CopySurveyDialog';
import type { SurveyDto, SurveyQuestionDto, CreateSurveyRequest } from './types';

export default function SurveysPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();

  // Create dialog state
  const [createOpen, setCreateOpen] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newDescription, setNewDescription] = useState('');
  const [newEndsAt, setNewEndsAt] = useState('');
  const [newAnonymous, setNewAnonymous] = useState(false);

  // Confirm actions
  const [launchTarget, setLaunchTarget] = useState<SurveyDto | null>(null);
  const [closeTarget, setCloseTarget] = useState<SurveyDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<SurveyDto | null>(null);

  // Copy dialog
  const [copyTarget, setCopyTarget] = useState<SurveyDto | null>(null);
  const [copyQuestions, setCopyQuestions] = useState<SurveyQuestionDto[]>([]);

  const invalidate = () => qc.invalidateQueries({ queryKey: ['surveys'] });

  const { data, isLoading, error } = useQuery({
    queryKey: ['surveys', 'list'],
    queryFn: () => surveysApi.getSurveys({ pageNumber: 1, pageSize: 100 }),
  });

  const createMut = useMutation({
    mutationFn: (req: CreateSurveyRequest) => surveysApi.createSurvey(req),
    onSuccess: () => { invalidate(); setCreateOpen(false); resetCreateForm(); },
  });

  const launchMut = useMutation({
    mutationFn: (id: string) => surveysApi.launchSurvey(id),
    onSuccess: () => { invalidate(); setLaunchTarget(null); },
  });

  const closeMut = useMutation({
    mutationFn: (id: string) => surveysApi.closeSurvey(id),
    onSuccess: () => { invalidate(); setCloseTarget(null); },
  });

  const copyMut = useMutation({
    mutationFn: ({ id, title, excludeIds }: { id: string; title: string; excludeIds: string[] }) =>
      surveysApi.copySurvey(id, { newTitle: title, excludeQuestionIds: excludeIds }),
    onSuccess: () => { invalidate(); setCopyTarget(null); },
  });

  const deleteMut = useMutation({
    mutationFn: (id: string) => surveysApi.deleteSurvey(id),
    onSuccess: () => { invalidate(); setDeleteTarget(null); },
  });

  function resetCreateForm() {
    setNewTitle('');
    setNewDescription('');
    setNewEndsAt('');
    setNewAnonymous(false);
  }

  function handleCreate() {
    if (!newTitle.trim()) return;
    createMut.mutate({
      title: newTitle.trim(),
      description: newDescription.trim() || undefined,
      endsAt: newEndsAt ? new Date(newEndsAt).toISOString() : undefined,
      isAnonymous: newAnonymous,
    });
  }

  async function openCopyDialog(survey: SurveyDto) {
    setCopyTarget(survey);
    try {
      const detail = await surveysApi.getSurveyById(survey.id);
      setCopyQuestions(detail.questions ?? []);
    } catch {
      setCopyQuestions([]);
    }
  }

  const surveys: SurveyDto[] = data?.data ?? [];

  return (
    <Box>
      <PageHeader title="Surveys" subtitle="Manage organisational surveys" />

      {error && <Box sx={{ mb: 2 }}><ApiErrorAlert error={error} /></Box>}

      <Card>
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Questions</TableCell>
                <TableCell align="right">Invited</TableCell>
                <TableCell align="right">Responded</TableCell>
                <TableCell align="right">Rate %</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={8} align="center">
                    <CircularProgress size={24} />
                  </TableCell>
                </TableRow>
              ) : surveys.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} align="center">
                    <Typography variant="body2" color="text.secondary">
                      No surveys yet. Create one to get started.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                surveys.map(s => {
                  const rate = s.totalInvited ? Math.round(s.totalResponded / s.totalInvited * 100) : 0;
                  return (
                    <TableRow key={s.id} hover>
                      <TableCell>{s.title}</TableCell>
                      <TableCell><SurveyStatusChip status={s.status} /></TableCell>
                      <TableCell align="right">{s.questionCount}</TableCell>
                      <TableCell align="right">{s.totalInvited}</TableCell>
                      <TableCell align="right">{s.totalResponded}</TableCell>
                      <TableCell align="right">
                        <Chip
                          label={`${rate}%`}
                          size="small"
                          color={rate >= 70 ? 'success' : rate >= 40 ? 'warning' : 'default'}
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>{new Date(s.createdDate).toLocaleDateString()}</TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.5}>
                          <Tooltip title="Edit / Build">
                            <IconButton size="small" onClick={() => navigate(`/admin/surveys/${s.id}`)}>
                              <EditIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                          {s.status === 'Draft' && (
                            <Tooltip title="Launch">
                              <IconButton size="small" color="success" onClick={() => setLaunchTarget(s)}>
                                <RocketLaunchIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {s.status === 'Active' && (
                            <Tooltip title="Close">
                              <IconButton size="small" color="error" onClick={() => setCloseTarget(s)}>
                                <StopIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          <Tooltip title="Copy">
                            <IconButton size="small" onClick={() => openCopyDialog(s)}>
                              <ContentCopyIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Delete">
                            <IconButton size="small" color="error" onClick={() => setDeleteTarget(s)}>
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <CardContent>
          <Typography variant="caption" color="text.secondary">
            {surveys.length} survey{surveys.length !== 1 ? 's' : ''}
          </Typography>
        </CardContent>
      </Card>

      {/* FAB — create new */}
      <Fab
        color="primary"
        sx={{ position: 'fixed', bottom: 24, right: 24 }}
        onClick={() => setCreateOpen(true)}
      >
        <AddIcon />
      </Fab>

      {/* Create survey dialog */}
      <Dialog
        open={createOpen}
        onClose={createMut.isPending ? undefined : () => { setCreateOpen(false); resetCreateForm(); }}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create New Survey</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Title"
              required
              fullWidth
              value={newTitle}
              onChange={e => setNewTitle(e.target.value)}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={2}
              value={newDescription}
              onChange={e => setNewDescription(e.target.value)}
            />
            <TextField
              label="Survey End Date"
              type="date"
              fullWidth
              value={newEndsAt}
              onChange={e => setNewEndsAt(e.target.value)}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: new Date().toISOString().split('T')[0] } }}
              helperText="Invitation links will expire on this date. Leave blank for no fixed end date."
            />
            <FormControlLabel
              control={<Switch checked={newAnonymous} onChange={e => setNewAnonymous(e.target.checked)} />}
              label="Anonymous responses"
            />
            {createMut.isError && <ApiErrorAlert error={createMut.error} />}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setCreateOpen(false); resetCreateForm(); }} disabled={createMut.isPending}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleCreate}
            disabled={!newTitle.trim() || createMut.isPending}
            startIcon={createMut.isPending ? <CircularProgress size={16} /> : undefined}
          >
            {createMut.isPending ? 'Creating…' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Launch confirm */}
      <ConfirmDialog
        open={!!launchTarget}
        title="Launch Survey"
        message={`Launch "${launchTarget?.title}" to all active employees? This cannot be undone.`}
        confirmLabel="Launch"
        onConfirm={() => launchTarget && launchMut.mutate(launchTarget.id)}
        onCancel={() => { launchMut.reset(); setLaunchTarget(null); }}
        loading={launchMut.isPending}
      >
        {launchMut.isError && <ApiErrorAlert error={launchMut.error} />}
      </ConfirmDialog>

      {/* Close confirm */}
      <ConfirmDialog
        open={!!closeTarget}
        title="Close Survey"
        message={`Close "${closeTarget?.title}"? All remaining tokens will be invalidated.`}
        confirmLabel="Close Survey"
        onConfirm={() => closeTarget && closeMut.mutate(closeTarget.id)}
        onCancel={() => setCloseTarget(null)}
        loading={closeMut.isPending}
        danger
      />

      {/* Copy dialog */}
      <CopySurveyDialog
        open={!!copyTarget}
        survey={copyTarget}
        questions={copyQuestions}
        onClose={() => setCopyTarget(null)}
        onCopy={(title, excludeIds) =>
          copyTarget && copyMut.mutate({ id: copyTarget.id, title, excludeIds })
        }
        loading={copyMut.isPending}
      />

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Survey"
        message={
          deleteTarget?.status === 'Draft'
            ? `Delete "${deleteTarget.title}"? This cannot be undone.`
            : `"${deleteTarget?.title}" has already been launched${
                deleteTarget?.totalInvited
                  ? ` and invitations were sent to ${deleteTarget.totalInvited} user${deleteTarget.totalInvited !== 1 ? 's' : ''}`
                  : ''
              }. Deleting it will permanently remove all responses and data. Are you sure?`
        }
        confirmLabel="Delete"
        onConfirm={() => deleteTarget && deleteMut.mutate(deleteTarget.id)}
        onCancel={() => { deleteMut.reset(); setDeleteTarget(null); }}
        loading={deleteMut.isPending}
        danger
      >
        {deleteMut.isError && <ApiErrorAlert error={deleteMut.error} />}
      </ConfirmDialog>
    </Box>
  );
}
