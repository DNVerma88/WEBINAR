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
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ratingScaleApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { useToast } from '../../../shared/hooks/useToast';
import type { RatingScaleDto } from '../types';

const EMPTY = { code: '', name: '', numericValue: 0, displayOrder: 0 };

export function RatingScalesTab() {
  const qc = useQueryClient();
  const toast = useToast();
  const [dialog, setDialog] = useState<'create' | 'edit' | null>(null);
  const [editing, setEditing] = useState<RatingScaleDto | null>(null);
  const [form, setForm] = useState(EMPTY);
  const [mutError, setMutError] = useState<Error | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'rating-scales'],
    queryFn: ratingScaleApi.getScales,
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['assessment', 'rating-scales'] });

  const createMut = useMutation({
    mutationFn: () => ratingScaleApi.createScale(form),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Rating scale created.'); },
    onError: setMutError,
  });

  const updateMut = useMutation({
    mutationFn: () => ratingScaleApi.updateScale(editing!.id, {
      ...form, isActive: editing!.isActive, recordVersion: editing!.recordVersion,
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Rating scale updated.'); },
    onError: setMutError,
  });

  const deactivateMut = useMutation({
    mutationFn: (id: string) => ratingScaleApi.deactivateScale(id),
    onSuccess: () => { invalidate(); toast.success('Rating scale deactivated.'); },
    onError: () => toast.error('Failed to deactivate rating scale.'),
  });

  const openCreate = () => { setForm(EMPTY); setEditing(null); setMutError(null); setDialog('create'); };
  const openEdit = (s: RatingScaleDto) => {
    setForm({ code: s.code, name: s.name, numericValue: s.numericValue, displayOrder: s.displayOrder });
    setEditing(s); setMutError(null); setDialog('edit');
  };
  const handleSubmit = () => { dialog === 'create' ? createMut.mutate() : updateMut.mutate(); };
  const isBusy = createMut.isPending || updateMut.isPending;

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" mb={2}>
        <Typography variant="h6">Rating Scales ({data.length})</Typography>
        <Button variant="contained" size="small" onClick={openCreate}>+ New Scale</Button>
      </Stack>

      <Stack direction="row" flexWrap="wrap" gap={2}>
        {data.map((scale) => (
          <Card key={scale.id} variant="outlined" sx={{ minWidth: 200 }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
                <Typography variant="h5" fontWeight={700} color="primary">{scale.numericValue}</Typography>
                <Chip label={scale.isActive ? 'Active' : 'Inactive'} size="small"
                  color={scale.isActive ? 'success' : 'default'} />
              </Stack>
              <Typography fontWeight={600}>{scale.name}</Typography>
              <Typography variant="caption" color="text.secondary">Code: {scale.code} · Order: {scale.displayOrder}</Typography>
              <Stack direction="row" spacing={1} mt={1.5}>
                <Button size="small" variant="outlined" onClick={() => openEdit(scale)}>Edit</Button>
                {scale.isActive && (
                  <Button size="small" variant="outlined" color="error"
                    onClick={() => deactivateMut.mutate(scale.id)} disabled={deactivateMut.isPending}>
                    Deactivate
                  </Button>
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>

      <Dialog open={!!dialog} onClose={() => setDialog(null)} maxWidth="xs" fullWidth>
        <DialogTitle>{dialog === 'create' ? 'New Rating Scale' : 'Edit Rating Scale'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Code" value={form.code} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, code: e.target.value }))} />
            <TextField label="Name" value={form.name} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))} />
            <TextField label="Numeric Value" type="number" value={form.numericValue} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, numericValue: +e.target.value }))} />
            <TextField label="Display Order" type="number" value={form.displayOrder} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, displayOrder: +e.target.value }))} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button variant="contained" onClick={handleSubmit} disabled={isBusy || !form.code || !form.name}>
            {isBusy ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
