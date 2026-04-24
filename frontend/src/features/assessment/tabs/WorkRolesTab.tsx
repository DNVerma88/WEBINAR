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
  Switch,
  TextField,
  Typography,
} from '@/components/ui';
import { FormControlLabel } from '@mui/material';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workRoleApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { useToast } from '../../../shared/hooks/useToast';
import type { WorkRoleDto } from '../types';

const CREATE_EMPTY = { code: '', name: '', category: '', displayOrder: 0 };

export function WorkRolesTab() {
  const qc = useQueryClient();
  const toast = useToast();
  const [dialog, setDialog] = useState<'create' | 'edit' | null>(null);
  const [editing, setEditing] = useState<WorkRoleDto | null>(null);
  const [form, setForm] = useState(CREATE_EMPTY);
  const [editIsActive, setEditIsActive] = useState(true);
  const [mutError, setMutError] = useState<Error | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'work-roles'],
    queryFn: () => workRoleApi.getWorkRoles(),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['assessment', 'work-roles'] });

  const createMut = useMutation({
    mutationFn: () => workRoleApi.createWorkRole({
      code: form.code.trim().toUpperCase(),
      name: form.name.trim(),
      category: form.category.trim(),
      displayOrder: Number(form.displayOrder),
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Work role created.'); },
    onError: setMutError,
  });

  const updateMut = useMutation({
    mutationFn: () => workRoleApi.updateWorkRole(editing!.id, {
      name: form.name.trim(),
      category: form.category.trim(),
      displayOrder: Number(form.displayOrder),
      isActive: editIsActive,
      recordVersion: editing!.recordVersion,
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Work role updated.'); },
    onError: setMutError,
  });

  const openCreate = () => {
    setForm(CREATE_EMPTY);
    setEditing(null);
    setMutError(null);
    setDialog('create');
  };

  const openEdit = (r: WorkRoleDto) => {
    setForm({ code: r.code, name: r.name, category: r.category, displayOrder: r.displayOrder });
    setEditIsActive(r.isActive);
    setEditing(r);
    setMutError(null);
    setDialog('edit');
  };

  const field = (key: keyof typeof form) => ({
    value: form[key],
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setForm(f => ({ ...f, [key]: e.target.value })),
  });

  // Group roles by category for display
  const grouped = (data ?? []).reduce<Record<string, WorkRoleDto[]>>((acc, r) => {
    const cat = r.category || 'Uncategorized';
    (acc[cat] ??= []).push(r);
    return acc;
  }, {});

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6">Work Roles</Typography>
        <Button variant="contained" onClick={openCreate}>Add Work Role</Button>
      </Stack>

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      {Object.entries(grouped).sort(([a], [b]) => a.localeCompare(b)).map(([category, roles]) => (
        <Box key={category} mb={3}>
          <Typography variant="subtitle1" fontWeight={600} color="text.secondary" mb={1}>
            {category}
          </Typography>
          <Stack spacing={1}>
            {roles.sort((a, b) => a.displayOrder - b.displayOrder).map(r => (
              <Card key={r.id} variant="outlined">
                <CardContent sx={{ py: 1, '&:last-child': { pb: 1 } }}>
                  <Stack direction="row" alignItems="center" justifyContent="space-between">
                    <Stack direction="row" spacing={2} alignItems="center">
                      <Chip label={r.code} size="small" variant="outlined" />
                      <Typography>{r.name}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        Order: {r.displayOrder}
                      </Typography>
                    </Stack>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Chip
                        label={r.isActive ? 'Active' : 'Inactive'}
                        color={r.isActive ? 'success' : 'default'}
                        size="small"
                      />
                      <Button size="small" onClick={() => openEdit(r)}>Edit</Button>
                    </Stack>
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Stack>
        </Box>
      ))}

      {/* Create Dialog */}
      <Dialog open={dialog === 'create'} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Work Role</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Code *" {...field('code')} helperText="e.g. DEV-BE (auto-uppercased)" />
            <TextField label="Name *" {...field('name')} helperText="e.g. Developer - Backend" />
            <TextField label="Category" {...field('category')} helperText="e.g. Engineering, QA, Product" />
            <TextField label="Display Order" type="number" {...field('displayOrder')} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => createMut.mutate()}
            disabled={createMut.isPending || !form.code.trim() || !form.name.trim()}
          >
            Create
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={dialog === 'edit'} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Work Role — {editing?.code}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Name *" {...field('name')} />
            <TextField label="Category" {...field('category')} />
            <TextField label="Display Order" type="number" {...field('displayOrder')} />
            <FormControlLabel
              control={<Switch checked={editIsActive} onChange={e => setEditIsActive(e.target.checked)} />}
              label="Active"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => updateMut.mutate()}
            disabled={updateMut.isPending || !form.name.trim()}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
