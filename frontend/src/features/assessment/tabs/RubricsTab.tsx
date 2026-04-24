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
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { rubricApi, ratingScaleApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { useToast } from '../../../shared/hooks/useToast';
import type { RubricDefinitionDto } from '../types';

const EMPTY = { designationCode: '', ratingScaleId: '', behaviorDescription: '', processDescription: '', evidenceDescription: '', effectiveFrom: '', effectiveTo: '' };

export function RubricsTab() {
  const qc = useQueryClient();
  const toast = useToast();
  const [designation, setDesignation] = useState('');
  const [dialog, setDialog] = useState<'create' | 'edit' | null>(null);
  const [editing, setEditing] = useState<RubricDefinitionDto | null>(null);
  const [form, setForm] = useState(EMPTY);
  const [mutError, setMutError] = useState<Error | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'rubrics', designation],
    queryFn: () => rubricApi.getRubrics(designation || undefined),
  });

  const { data: scales } = useQuery({
    queryKey: ['assessment', 'rating-scales'],
    queryFn: ratingScaleApi.getScales,
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['assessment', 'rubrics'] });

  const createMut = useMutation({
    mutationFn: () => rubricApi.createRubric({
      designationCode: form.designationCode,
      ratingScaleId: form.ratingScaleId as never,
      behaviorDescription: form.behaviorDescription,
      processDescription: form.processDescription,
      evidenceDescription: form.evidenceDescription,
      effectiveFrom: form.effectiveFrom as never,
      effectiveTo: (form.effectiveTo || undefined) as never,
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Rubric created.'); },
    onError: setMutError,
  });

  const updateMut = useMutation({
    mutationFn: () => rubricApi.updateRubric(editing!.id, {
      behaviorDescription: form.behaviorDescription,
      processDescription: form.processDescription,
      evidenceDescription: form.evidenceDescription,
      effectiveFrom: form.effectiveFrom as never,
      effectiveTo: (form.effectiveTo || undefined) as never,
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Rubric updated.'); },
    onError: setMutError,
  });

  const openCreate = () => { setForm(EMPTY); setEditing(null); setMutError(null); setDialog('create'); };
  const openEdit = (r: RubricDefinitionDto) => {
    setForm({
      designationCode: r.designationCode, ratingScaleId: r.ratingScaleId,
      behaviorDescription: r.behaviorDescription, processDescription: r.processDescription,
      evidenceDescription: r.evidenceDescription,
      effectiveFrom: r.effectiveFrom as unknown as string,
      effectiveTo: (r.effectiveTo ?? '') as unknown as string,
    });
    setEditing(r); setMutError(null); setDialog('edit');
  };
  const isBusy = createMut.isPending || updateMut.isPending;

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" spacing={2} mb={3} alignItems="center" justifyContent="space-between">
        <Stack direction="row" spacing={2} alignItems="center">
          <Typography variant="h6">Rubric Definitions</Typography>
          <TextField
            size="small"
            placeholder="Filter by designation code…"
            value={designation}
            onChange={(e) => setDesignation(e.target.value)}
            sx={{ width: 240 }}
          />
        </Stack>
        <Button variant="contained" size="small" onClick={openCreate}>+ New Rubric</Button>
      </Stack>

      <Stack spacing={2}>
        {data?.map((rubric) => (
          <Card key={rubric.id} variant="outlined">
            <CardContent>
              <Stack direction="row" justifyContent="space-between" mb={1}>
                <Typography fontWeight={600}>{rubric.designationCode}</Typography>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Chip label={`${rubric.ratingScaleValue} – ${rubric.ratingScaleName}`} size="small" color="primary" />
                  <Chip label={`v${rubric.versionNo}`} size="small" variant="outlined" />
                  <Chip label={rubric.isActive ? 'Active' : 'Inactive'} size="small"
                    color={rubric.isActive ? 'success' : 'default'} />
                  <Button size="small" variant="outlined" onClick={() => openEdit(rubric)}>Edit</Button>
                </Stack>
              </Stack>
              <Typography variant="body2"><strong>Behavior:</strong> {rubric.behaviorDescription}</Typography>
              <Typography variant="body2" mt={0.5}><strong>Process:</strong> {rubric.processDescription}</Typography>
              <Typography variant="body2" mt={0.5}><strong>Evidence:</strong> {rubric.evidenceDescription}</Typography>
              <Typography variant="caption" color="text.secondary" mt={1} display="block">
                Effective: {rubric.effectiveFrom}{rubric.effectiveTo ? ` → ${rubric.effectiveTo}` : ' (no end date)'}
              </Typography>
            </CardContent>
          </Card>
        ))}
        {data?.length === 0 && (
          <Typography color="text.secondary">No rubrics found for the given filter.</Typography>
        )}
      </Stack>

      <Dialog open={!!dialog} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>{dialog === 'create' ? 'New Rubric Definition' : 'Edit Rubric'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            {dialog === 'create' && (
              <TextField label="Designation Code" value={form.designationCode} required fullWidth
                onChange={(e) => setForm(f => ({ ...f, designationCode: e.target.value }))} />
            )}
            {dialog === 'create' && (
              <FormControl fullWidth required>
                <InputLabel>Rating Scale</InputLabel>
                <Select label="Rating Scale" value={form.ratingScaleId}
                  onChange={(e) => setForm(f => ({ ...f, ratingScaleId: e.target.value as string }))}>
                  {scales?.filter(s => s.isActive).map(s => (
                    <MenuItem key={s.id} value={s.id}>{s.numericValue} – {s.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
            <TextField label="Behavior Description" value={form.behaviorDescription} required fullWidth multiline rows={2}
              onChange={(e) => setForm(f => ({ ...f, behaviorDescription: e.target.value }))} />
            <TextField label="Process Description" value={form.processDescription} required fullWidth multiline rows={2}
              onChange={(e) => setForm(f => ({ ...f, processDescription: e.target.value }))} />
            <TextField label="Evidence Description" value={form.evidenceDescription} required fullWidth multiline rows={2}
              onChange={(e) => setForm(f => ({ ...f, evidenceDescription: e.target.value }))} />
            <TextField label="Effective From (YYYY-MM-DD)" value={form.effectiveFrom} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, effectiveFrom: e.target.value }))} />
            <TextField label="Effective To (optional)" value={form.effectiveTo} fullWidth
              onChange={(e) => setForm(f => ({ ...f, effectiveTo: e.target.value }))} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button variant="contained" onClick={() => dialog === 'create' ? createMut.mutate() : updateMut.mutate()}
            disabled={isBusy || !form.behaviorDescription || !form.effectiveFrom}>
            {isBusy ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
