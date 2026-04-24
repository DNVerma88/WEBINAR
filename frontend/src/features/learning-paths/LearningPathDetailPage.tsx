import { useState } from 'react';
import {
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  FormControlLabel,
  IconButton,
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
import { AddIcon, DeleteIcon, EditIcon, GroupsIcon } from '@/components/ui';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { learningPathsApi } from '../../shared/api/learningPaths';
import { sessionsApi } from '../../shared/api/sessions';
import { knowledgeAssetsApi } from '../../shared/api/knowledgeAssets';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';
import {
  DifficultyLevelLabel,
  CohortStatusLabel,
  LearningPathItemType,
  CohortStatus,
  type LearningPathCohortDto,
  type AddLearningPathItemRequest,
  type CreateLearningPathCohortRequest,
  type UpdateLearningPathCohortRequest,
} from '../../shared/types';

interface CohortFormValues {
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  maxParticipants: string;
}

export default function LearningPathDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { user } = useAuth();
  usePageTitle('Learning Path');

  const [tab, setTab] = useState(0);

  // Cohort dialog state
  const [cohortDialogOpen, setCohortDialogOpen] = useState(false);
  const [editingCohort, setEditingCohort] = useState<LearningPathCohortDto | null>(null);
  const [deleteCohortId, setDeleteCohortId] = useState<string | null>(null);

  // Item management state
  const [addItemOpen, setAddItemOpen] = useState(false);
  const [addItemType, setAddItemType] = useState<LearningPathItemType>(LearningPathItemType.Session);
  const [addItemId, setAddItemId] = useState('');
  const [addItemSearch, setAddItemSearch] = useState('');
  const [addItemRequired, setAddItemRequired] = useState(true);

  const isAdmin = (user?.role ?? 0) >= 16;
  const isKt = ((user?.role ?? 0) & 8) !== 0;
  const canManageCohorts = isAdmin || isKt;

  const { data: path, isLoading, error } = useQuery({
    queryKey: ['learning-paths', id],
    queryFn: ({ signal }) => learningPathsApi.getPathById(id!, signal),
    enabled: !!id,
  });

  const { data: progress } = useQuery({
    queryKey: ['learning-path-progress', id],
    queryFn: ({ signal }) => learningPathsApi.getProgress(id!, signal),
    enabled: !!id,
    retry: false,
  });

  const { data: cohorts = [] } = useQuery({
    queryKey: ['learning-path-cohorts', id],
    queryFn: ({ signal }) => learningPathsApi.getCohorts(id!, signal),
    enabled: !!id,
  });

  const toast = useToast();
  const enrolMutation = useMutation({
    mutationFn: () => learningPathsApi.enrol(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['learning-path-progress', id] }); toast.success('Enrolled successfully.'); },
    onError: () => toast.error('Failed to enrol.'),
  });

  const unenrolMutation = useMutation({
    mutationFn: () => learningPathsApi.unenrol(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['learning-path-progress', id] }); toast.success('Unenrolled successfully.'); },
    onError: () => toast.error('Failed to unenrol.'),
  });

  const createCohortMutation = useMutation({
    mutationFn: (data: CreateLearningPathCohortRequest) => learningPathsApi.createCohort(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['learning-path-cohorts', id] });
      setCohortDialogOpen(false);
      toast.success('Cohort created.');
    },
    onError: () => toast.error('Failed to create cohort.'),
  });

  const updateCohortMutation = useMutation({
    mutationFn: ({ cohortId, data }: { cohortId: string; data: UpdateLearningPathCohortRequest }) =>
      learningPathsApi.updateCohort(id!, cohortId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['learning-path-cohorts', id] });
      setCohortDialogOpen(false);
      setEditingCohort(null);
      toast.success('Cohort updated.');
    },
    onError: () => toast.error('Failed to update cohort.'),
  });

  const deleteCohortMutation = useMutation({
    mutationFn: (cohortId: string) => learningPathsApi.deleteCohort(id!, cohortId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['learning-path-cohorts', id] });
      setDeleteCohortId(null);
      toast.success('Cohort deleted.');
    },
    onError: () => toast.error('Failed to delete cohort.'),
  });

  const addItemMutation = useMutation({
    mutationFn: (data: AddLearningPathItemRequest) => learningPathsApi.addItem(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['learning-paths', id] });
      setAddItemOpen(false);
      setAddItemId('');
      setAddItemSearch('');
      setAddItemType(LearningPathItemType.Session);
      setAddItemRequired(true);
      toast.success('Item added to learning path.');
    },
    onError: () => toast.error('Failed to add item.'),
  });

  const { data: sessionsList } = useQuery({
    queryKey: ['sessions-for-lp', addItemSearch],
    queryFn: ({ signal }) => sessionsApi.getSessions({ searchTerm: addItemSearch || undefined, pageSize: 50 }, signal),
    enabled: addItemOpen && addItemType === LearningPathItemType.Session,
  });

  const { data: assetsList } = useQuery({
    queryKey: ['assets-for-lp', addItemSearch],
    queryFn: ({ signal }) => knowledgeAssetsApi.getAssets({ searchTerm: addItemSearch || undefined, pageSize: 50 }, signal),
    enabled: addItemOpen && addItemType === LearningPathItemType.KnowledgeAsset,
  });

  const removeItemMutation = useMutation({
    mutationFn: (itemId: string) => learningPathsApi.removeItem(id!, itemId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['learning-paths', id] }); toast.success('Item removed.'); },
    onError: () => toast.error('Failed to remove item.'),
  });

  const cohortForm = useForm<CohortFormValues>({
    defaultValues: { name: '', description: '', startDate: '', endDate: '', maxParticipants: '' },
  });

  const openCreateCohort = () => {
    setEditingCohort(null);
    cohortForm.reset({ name: '', description: '', startDate: '', endDate: '', maxParticipants: '' });
    setCohortDialogOpen(true);
  };

  const openEditCohort = (c: LearningPathCohortDto) => {
    setEditingCohort(c);
    cohortForm.reset({
      name: c.name,
      description: c.description ?? '',
      startDate: c.startDate ? c.startDate.slice(0, 10) : '',
      endDate: c.endDate ? c.endDate.slice(0, 10) : '',
      maxParticipants: c.maxParticipants != null ? String(c.maxParticipants) : '',
    });
    setCohortDialogOpen(true);
  };

  const handleCohortSubmit = (vals: CohortFormValues) => {
    const base = {
      name: vals.name.trim(),
      description: vals.description.trim() || undefined,
      startDate: new Date(vals.startDate).toISOString(),
      endDate: vals.endDate ? new Date(vals.endDate).toISOString() : undefined,
      maxParticipants: vals.maxParticipants ? parseInt(vals.maxParticipants, 10) : undefined,
    };
    if (editingCohort) {
      updateCohortMutation.mutate({ cohortId: editingCohort.id, data: { ...base, status: editingCohort.status } });
    } else {
      createCohortMutation.mutate(base);
    }
  };

  const cohortStatusColor = (status: string): 'default' | 'success' | 'error' | 'info' => {
    if (status === CohortStatus.Active) return 'success';
    if (status === CohortStatus.Completed) return 'info';
    if (status === CohortStatus.Cancelled) return 'error';
    return 'default';
  };

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!path) return null;

  const isEnrolled = !!progress;

  return (
    <Box>
      <PageHeader
        title={path.title}
        subtitle={path.objective ?? path.description ?? ''}
        breadcrumbs={[{ label: 'Learning Paths', to: '/learning-paths' }, { label: path.title }]}
        actions={
          isEnrolled ? (
            <Button variant="outlined" color="error" onClick={() => unenrolMutation.mutate()} disabled={unenrolMutation.isPending}>
              Unenrol
            </Button>
          ) : (
            <Button variant="contained" onClick={() => enrolMutation.mutate()} disabled={enrolMutation.isPending}>
              Enrol Now
            </Button>
          )
        }
      />

      <Box display="flex" gap={2} mb={2} flexWrap="wrap">
        <Chip label={DifficultyLevelLabel[path.difficultyLevel]} color="primary" />
        {path.categoryName && <Chip label={path.categoryName} variant="outlined" />}
        <Chip label={`${path.itemCount} items`} variant="outlined" />
        {path.estimatedDurationMinutes > 0 && (
          <Chip label={`${path.estimatedDurationMinutes} min`} variant="outlined" />
        )}
        {!path.isPublished && <Chip label="Draft" color="warning" />}
      </Box>

      {isEnrolled && progress && (
        <Box mb={2} p={2} sx={{ bgcolor: 'primary.light', borderRadius: 2 }}>
          <Typography variant="subtitle1" fontWeight={700} gutterBottom>Your Progress</Typography>
          <Box display="flex" gap={2} alignItems="center">
            <Box flexGrow={1} bgcolor="background.paper" borderRadius={1} height={10} overflow="hidden">
              <Box width={`${progress.progressPercentage}%`} bgcolor="primary.main" height="100%" />
            </Box>
            <Typography variant="body2" fontWeight={600}>{Math.round(progress.progressPercentage)}%</Typography>
            <Typography variant="body2" color="text.secondary">
              {progress.completedItemCount}/{progress.totalItemCount} items
            </Typography>
          </Box>
        </Box>
      )}

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tab label="Overview" />
        <Tab label={`Cohorts (${cohorts.length})`} icon={<GroupsIcon fontSize="small" />} iconPosition="start" />
      </Tabs>

      {/* ── Overview Tab ── */}
      {tab === 0 && (
        <Box>
          {path.description && (
            <Typography variant="body1" mb={3} sx={{ whiteSpace: 'pre-line' }}>
              {path.description}
            </Typography>
          )}
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6" fontWeight={700}>Content ({path.items.length} items)</Typography>
            {canManageCohorts && (
              <Button variant="contained" size="small" startIcon={<AddIcon />} onClick={() => setAddItemOpen(true)}>
                Add Item
              </Button>
            )}
          </Box>
          <Stack spacing={1}>
            {path.items.map((item, index) => (
              <Box key={item.id}>
                <Box display="flex" alignItems="center" gap={2} py={1.5}>
                  <Typography variant="body2" color="text.secondary" sx={{ minWidth: 24 }}>{index + 1}</Typography>
                  <Box flexGrow={1}>
                    <Typography variant="body1" fontWeight={500}>
                      {item.sessionTitle ?? item.assetTitle ?? 'Item'}
                    </Typography>
                    <Box display="flex" gap={1} mt={0.5}>
                      <Chip label={item.itemType === LearningPathItemType.Session ? 'Session' : 'Asset'} size="small" variant="outlined" />
                      {item.isRequired && <Chip label="Required" size="small" color="error" />}
                    </Box>
                  </Box>
                  {canManageCohorts && (
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => removeItemMutation.mutate(item.id)}
                      disabled={removeItemMutation.isPending}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  )}
                </Box>
                {index < path.items.length - 1 && <Divider />}
              </Box>
            ))}
            {path.items.length === 0 && <Typography color="text.secondary">No content items added yet.</Typography>}
          </Stack>
        </Box>
      )}

      {/* ── Cohorts Tab ── */}
      {tab === 1 && (
        <Box>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6" fontWeight={700}>Cohorts</Typography>
            {canManageCohorts && (
              <Button variant="contained" startIcon={<AddIcon />} size="small" onClick={openCreateCohort}>
                Add Cohort
              </Button>
            )}
          </Box>

          {cohorts.length === 0 ? (
            <Typography color="text.secondary">No cohorts have been created for this learning path yet.</Typography>
          ) : (
            <Stack spacing={2}>
              {cohorts.map((c) => (
                <Paper key={c.id} variant="outlined" sx={{ p: 2 }}>
                  <Box display="flex" justifyContent="space-between" alignItems="flex-start">
                    <Box>
                      <Box display="flex" alignItems="center" gap={1} mb={0.5}>
                        <Typography variant="subtitle1" fontWeight={600}>{c.name}</Typography>
                        <Chip
                          label={CohortStatusLabel[c.status]}
                          size="small"
                          color={cohortStatusColor(c.status)}
                        />
                      </Box>
                      {c.description && (
                        <Typography variant="body2" color="text.secondary" mb={1}>{c.description}</Typography>
                      )}
                      <Box display="flex" gap={2} flexWrap="wrap">
                        <Typography variant="caption" color="text.secondary">
                          Starts: {new Date(c.startDate).toLocaleDateString()}
                        </Typography>
                        {c.endDate && (
                          <Typography variant="caption" color="text.secondary">
                            Ends: {new Date(c.endDate).toLocaleDateString()}
                          </Typography>
                        )}
                        {c.maxParticipants != null && (
                          <Typography variant="caption" color="text.secondary">
                            Max: {c.maxParticipants} participants
                          </Typography>
                        )}
                      </Box>
                    </Box>
                    {canManageCohorts && (
                      <Box display="flex" gap={1}>
                        <Button size="small" startIcon={<EditIcon />} onClick={() => openEditCohort(c)}>Edit</Button>
                        <Button size="small" color="error" startIcon={<DeleteIcon />} onClick={() => setDeleteCohortId(c.id)}>
                          Delete
                        </Button>
                      </Box>
                    )}
                  </Box>
                </Paper>
              ))}
            </Stack>
          )}
        </Box>
      )}

      {/* ── Create/Edit Cohort Dialog ── */}
      <Dialog open={cohortDialogOpen} onClose={() => setCohortDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editingCohort ? 'Edit Cohort' : 'Add Cohort'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Name"
              required
              fullWidth
              {...cohortForm.register('name', { required: true })}
              error={!!cohortForm.formState.errors.name}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={2}
              {...cohortForm.register('description')}
            />
            <TextField
              label="Start Date"
              type="date"
              required
              fullWidth
              InputLabelProps={{ shrink: true }}
              {...cohortForm.register('startDate', { required: true })}
              error={!!cohortForm.formState.errors.startDate}
            />
            <TextField
              label="End Date"
              type="date"
              fullWidth
              InputLabelProps={{ shrink: true }}
              {...cohortForm.register('endDate')}
            />
            <TextField
              label="Max Participants"
              type="number"
              fullWidth
              inputProps={{ min: 1 }}
              {...cohortForm.register('maxParticipants')}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCohortDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={cohortForm.handleSubmit(handleCohortSubmit)}
            disabled={createCohortMutation.isPending || updateCohortMutation.isPending}
          >
            {editingCohort ? 'Save' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Delete Cohort Confirm ── */}
      <Dialog open={!!deleteCohortId} onClose={() => setDeleteCohortId(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Delete Cohort</DialogTitle>
        <DialogContent>
          <Typography>Are you sure you want to delete this cohort? This cannot be undone.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteCohortId(null)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => deleteCohortId && deleteCohortMutation.mutate(deleteCohortId)}
            disabled={deleteCohortMutation.isPending}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Add Item Dialog ── */}
      <Dialog open={addItemOpen} onClose={() => { setAddItemOpen(false); setAddItemId(''); setAddItemSearch(''); }} maxWidth="sm" fullWidth>
        <DialogTitle>Add Content Item</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <FormControl fullWidth>
              <InputLabel>Item Type</InputLabel>
              <Select
                value={addItemType}
                label="Item Type"
                onChange={(e) => { setAddItemType(e.target.value as LearningPathItemType); setAddItemId(''); setAddItemSearch(''); }}
              >
                <MenuItem value={LearningPathItemType.Session}>Session</MenuItem>
                <MenuItem value={LearningPathItemType.KnowledgeAsset}>Knowledge Asset</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Search"
              value={addItemSearch}
              onChange={(e) => { setAddItemSearch(e.target.value); setAddItemId(''); }}
              fullWidth
              placeholder={addItemType === LearningPathItemType.Session ? 'Search sessions…' : 'Search assets…'}
              size="small"
            />
            <FormControl fullWidth required>
              <InputLabel>{addItemType === LearningPathItemType.Session ? 'Select Session' : 'Select Asset'}</InputLabel>
              <Select
                value={addItemId}
                label={addItemType === LearningPathItemType.Session ? 'Select Session' : 'Select Asset'}
                onChange={(e) => setAddItemId(e.target.value)}
              >
                {addItemType === LearningPathItemType.Session
                  ? (sessionsList?.data ?? []).map((s) => (
                      <MenuItem key={s.id} value={s.id}>{s.title}</MenuItem>
                    ))
                  : (assetsList?.data ?? []).map((a) => (
                      <MenuItem key={a.id} value={a.id}>{a.title}</MenuItem>
                    ))
                }
              </Select>
            </FormControl>
            <FormControlLabel
              control={
                <Checkbox
                  checked={addItemRequired}
                  onChange={(e) => setAddItemRequired(e.target.checked)}
                />
              }
              label="Required for path completion"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setAddItemOpen(false); setAddItemId(''); setAddItemSearch(''); }}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!addItemId.trim() || addItemMutation.isPending}
            onClick={() =>
              addItemMutation.mutate({
                itemType: addItemType,
                sessionId: addItemType === LearningPathItemType.Session ? addItemId.trim() : undefined,
                knowledgeAssetId: addItemType === LearningPathItemType.KnowledgeAsset ? addItemId.trim() : undefined,
                isRequired: addItemRequired,
              })
            }
          >
            Add
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
