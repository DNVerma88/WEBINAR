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
  Divider,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { parameterApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { useToast } from '../../../shared/hooks/useToast';
import type { ParameterMasterDto, RoleParameterMappingDto } from '../types';

const EMPTY_P = { name: '', code: '', description: '', category: '', displayOrder: 0 };
const EMPTY_M = { designationCode: '', parameterId: '', weightage: 1, displayOrder: 0, isMandatory: false };

type DialogMode = 'create' | 'edit' | 'mappings' | null;

export function ParametersTab() {
  const qc = useQueryClient();
  const toast = useToast();
  const [dialog, setDialog] = useState<DialogMode>(null);
  const [editing, setEditing] = useState<ParameterMasterDto | null>(null);
  const [form, setForm] = useState(EMPTY_P);
  const [mapForm, setMapForm] = useState(EMPTY_M);
  const [mapDesig, setMapDesig] = useState('');
  const [mutError, setMutError] = useState<Error | null>(null);

  const { data: params, isLoading, error } = useQuery({
    queryKey: ['assessment', 'parameters'],
    queryFn: parameterApi.getParameters,
  });

  const { data: mappings, isLoading: loadingMaps } = useQuery({
    queryKey: ['assessment', 'role-mappings', mapDesig],
    queryFn: () => parameterApi.getRoleMappings(mapDesig || undefined),
    enabled: dialog === 'mappings',
  });

  const invalidateParams = () => qc.invalidateQueries({ queryKey: ['assessment', 'parameters'] });
  const invalidateMaps   = () => qc.invalidateQueries({ queryKey: ['assessment', 'role-mappings'] });

  const createMut = useMutation({
    mutationFn: () => parameterApi.createParameter(form),
    onSuccess: () => { invalidateParams(); setDialog(null); setMutError(null); toast.success('Parameter created.'); },
    onError: setMutError,
  });

  const updateMut = useMutation({
    mutationFn: () => parameterApi.updateParameter(editing!.id, {
      name: form.name, description: form.description || undefined,
      category: form.category, displayOrder: form.displayOrder,
      isActive: editing!.isActive, recordVersion: editing!.recordVersion,
    }),
    onSuccess: () => { invalidateParams(); setDialog(null); setMutError(null); toast.success('Parameter updated.'); },
    onError: setMutError,
  });

  const upsertMapMut = useMutation({
    mutationFn: () => parameterApi.upsertRoleMapping({
      designationCode: mapForm.designationCode, parameterId: mapForm.parameterId as never,
      weightage: mapForm.weightage, displayOrder: mapForm.displayOrder, isMandatory: mapForm.isMandatory,
    }),
    onSuccess: () => { invalidateMaps(); setMapForm(EMPTY_M); setMutError(null); toast.success('Role mapping saved.'); },
    onError: setMutError,
  });

  const removeMapMut = useMutation({
    mutationFn: (id: string) => parameterApi.removeRoleMapping(id),
    onSuccess: () => { invalidateMaps(); toast.success('Mapping removed.'); },
    onError: () => toast.error('Failed to remove mapping.'),
  });

  const openCreate = () => { setForm(EMPTY_P); setEditing(null); setMutError(null); setDialog('create'); };
  const openEdit = (p: ParameterMasterDto) => {
    setForm({ name: p.name, code: p.code, description: p.description ?? '', category: p.category, displayOrder: p.displayOrder });
    setEditing(p); setMutError(null); setDialog('edit');
  };
  const isBusy = createMut.isPending || updateMut.isPending;

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" mb={2}>
        <Typography variant="h6">Assessment Parameters ({params?.length ?? 0})</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" size="small" onClick={() => { setMapDesig(''); setMapForm(EMPTY_M); setMutError(null); setDialog('mappings'); }}>
            Role Mappings
          </Button>
          <Button variant="contained" size="small" onClick={openCreate}>+ New Parameter</Button>
        </Stack>
      </Stack>

      <Stack spacing={1}>
        {params?.map((p) => (
          <Card key={p.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography fontWeight={600}>{p.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Code: {p.code} · Category: {p.category} · Order: {p.displayOrder}
                  </Typography>
                  {p.description && <Typography variant="body2" mt={0.5}>{p.description}</Typography>}
                </Box>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Chip label={p.isActive ? 'Active' : 'Inactive'} size="small"
                    color={p.isActive ? 'success' : 'default'} />
                  <Button size="small" variant="outlined" onClick={() => openEdit(p)}>Edit</Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>

      {/* Create / Edit Parameter Dialog */}
      <Dialog open={dialog === 'create' || dialog === 'edit'} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>{dialog === 'create' ? 'New Parameter' : 'Edit Parameter'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Name" value={form.name} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))} />
            {dialog === 'create' && (
              <TextField label="Code" value={form.code} required fullWidth
                onChange={(e) => setForm(f => ({ ...f, code: e.target.value }))} />
            )}
            <TextField label="Category" value={form.category} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, category: e.target.value }))} />
            <TextField label="Description" value={form.description} fullWidth multiline rows={2}
              onChange={(e) => setForm(f => ({ ...f, description: e.target.value }))} />
            <TextField label="Display Order" type="number" value={form.displayOrder} fullWidth
              onChange={(e) => setForm(f => ({ ...f, displayOrder: +e.target.value }))} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button variant="contained" onClick={() => dialog === 'create' ? createMut.mutate() : updateMut.mutate()}
            disabled={isBusy || !form.name || !form.category}>
            {isBusy ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Role Mappings Dialog */}
      <Dialog open={dialog === 'mappings'} onClose={() => setDialog(null)} maxWidth="md" fullWidth>
        <DialogTitle>Role → Parameter Mappings</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <Typography variant="subtitle2">Add / Update Mapping</Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <TextField label="Designation Code" value={mapForm.designationCode} required size="small"
                onChange={(e) => setMapForm(f => ({ ...f, designationCode: e.target.value }))} />
              <FormControl size="small" sx={{ minWidth: 200 }} required>
                <InputLabel>Parameter</InputLabel>
                <Select label="Parameter" value={mapForm.parameterId}
                  onChange={(e) => setMapForm(f => ({ ...f, parameterId: e.target.value as string }))}>
                  {params?.filter(p => p.isActive).map(p => (
                    <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField label="Weightage" type="number" value={mapForm.weightage} size="small" sx={{ width: 100 }}
                onChange={(e) => setMapForm(f => ({ ...f, weightage: +e.target.value }))} />
              <TextField label="Order" type="number" value={mapForm.displayOrder} size="small" sx={{ width: 80 }}
                onChange={(e) => setMapForm(f => ({ ...f, displayOrder: +e.target.value }))} />
              <FormControlLabel label="Mandatory"
                control={<Switch checked={mapForm.isMandatory}
                  onChange={(e) => setMapForm(f => ({ ...f, isMandatory: e.target.checked }))} />} />
              <Button variant="contained" size="small"
                disabled={upsertMapMut.isPending || !mapForm.designationCode || !mapForm.parameterId}
                onClick={() => upsertMapMut.mutate()}>
                Save Mapping
              </Button>
            </Stack>

            <Divider />

            <Stack direction="row" spacing={1} alignItems="center">
              <TextField label="Filter by designation" size="small" value={mapDesig}
                onChange={(e) => setMapDesig(e.target.value)} />
            </Stack>

            {loadingMaps ? <LoadingOverlay /> : (
              <Stack spacing={1}>
                {mappings?.map((m: RoleParameterMappingDto) => (
                  <Stack key={m.id} direction="row" justifyContent="space-between" alignItems="center"
                    sx={{ p: 1, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                    <Box>
                      <Typography variant="body2" fontWeight={600}>
                        {m.designationCode} → {m.parameterName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Weightage: {m.weightage} · Order: {m.displayOrder}
                        {m.isMandatory ? ' · Mandatory' : ''}
                      </Typography>
                    </Box>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Chip label={m.isActive ? 'Active' : 'Inactive'} size="small"
                        color={m.isActive ? 'success' : 'default'} />
                      <Button size="small" color="error" onClick={() => removeMapMut.mutate(m.id)}>Remove</Button>
                    </Stack>
                  </Stack>
                ))}
                {mappings?.length === 0 && <Typography variant="body2" color="text.secondary">No mappings found.</Typography>}
              </Stack>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
