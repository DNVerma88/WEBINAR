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
import { assessmentPeriodApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { AssessmentPeriodStatus, AssessmentPeriodFrequency } from '../types';
import type { AssessmentPeriodDto } from '../types';
import { useToast } from '../../../shared/hooks/useToast';

const STATUS_LABELS: Record<AssessmentPeriodStatus, string> = {
  [AssessmentPeriodStatus.Draft]:     'Draft',
  [AssessmentPeriodStatus.Open]:      'Open',
  [AssessmentPeriodStatus.Closed]:    'Closed',
  [AssessmentPeriodStatus.Published]: 'Published',
};

const FREQUENCY_LABELS: Record<AssessmentPeriodFrequency, string> = {
  [AssessmentPeriodFrequency.Weekly]:     'Weekly',
  [AssessmentPeriodFrequency.BiWeekly]:   'Bi-Weekly',
  [AssessmentPeriodFrequency.Monthly]:    'Monthly',
  [AssessmentPeriodFrequency.Quarterly]:  'Quarterly',
  [AssessmentPeriodFrequency.HalfYearly]: 'Half-Yearly',
  [AssessmentPeriodFrequency.Annual]:     'Annual',
};
const STATUS_COLORS: Record<AssessmentPeriodStatus, 'default' | 'success' | 'warning' | 'info'> = {
  [AssessmentPeriodStatus.Draft]:     'default',
  [AssessmentPeriodStatus.Open]:      'success',
  [AssessmentPeriodStatus.Closed]:    'warning',
  [AssessmentPeriodStatus.Published]: 'info',
};

const EMPTY_FORM = { name: '', frequency: AssessmentPeriodFrequency.Weekly, startDate: '', endDate: '', year: new Date().getFullYear(), weekNumber: '' };
const EMPTY_GEN  = { year: new Date().getFullYear(), frequency: AssessmentPeriodFrequency.Weekly };

type DialogMode = 'create' | 'edit' | 'generate' | null;

export function PeriodsTab() {
  const qc = useQueryClient();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [dialog, setDialog] = useState<DialogMode>(null);
  const [editing, setEditing] = useState<AssessmentPeriodDto | null>(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [genForm, setGenForm] = useState(EMPTY_GEN);
  const [mutError, setMutError] = useState<Error | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'periods', page],
    queryFn: () => assessmentPeriodApi.getPeriods({ pageNumber: page, pageSize: 20 }),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['assessment', 'periods'] });

  const openPeriodMut   = useMutation({ mutationFn: (id: string) => assessmentPeriodApi.openPeriod(id),    onSuccess: () => { invalidate(); toast.success('Period opened.'); }, onError: () => toast.error('Failed to open period.') });
  const closePeriodMut  = useMutation({ mutationFn: (id: string) => assessmentPeriodApi.closePeriod(id),   onSuccess: () => { invalidate(); toast.success('Period closed.'); }, onError: () => toast.error('Failed to close period.') });
  const publishPeriodMut = useMutation({ mutationFn: (id: string) => assessmentPeriodApi.publishPeriod(id), onSuccess: () => { invalidate(); toast.success('Period published.'); }, onError: () => toast.error('Failed to publish period.') });

  const createMut = useMutation({
    mutationFn: () => assessmentPeriodApi.createPeriod({
      name: form.name, frequency: form.frequency,
      startDate: form.startDate as never, endDate: form.endDate as never,
      year: form.year, weekNumber: form.weekNumber ? +form.weekNumber : undefined,
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Period created.'); },
    onError: setMutError,
  });

  const updateMut = useMutation({
    mutationFn: () => assessmentPeriodApi.updatePeriod(editing!.id, {
      name: form.name,
      startDate: form.startDate as never, endDate: form.endDate as never,
      weekNumber: form.weekNumber ? +form.weekNumber : undefined,
      recordVersion: editing!.recordVersion,
    }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Period updated.'); },
    onError: setMutError,
  });

  const generateMut = useMutation({
    mutationFn: () => assessmentPeriodApi.generatePeriods({ year: genForm.year, frequency: genForm.frequency }),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Periods generated successfully.'); },
    onError: setMutError,
  });

  const openCreate = () => { setForm(EMPTY_FORM); setEditing(null); setMutError(null); setDialog('create'); };
  const openEdit = (p: AssessmentPeriodDto) => {
    setForm({ name: p.name, frequency: p.frequency, startDate: p.startDate as unknown as string,
      endDate: p.endDate as unknown as string, year: p.year, weekNumber: p.weekNumber?.toString() ?? '' });
    setEditing(p); setMutError(null); setDialog('edit');
  };
  const isBusy = createMut.isPending || updateMut.isPending;

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" mb={2}>
        <Typography variant="h6">Assessment Periods ({data.totalCount})</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" size="small" onClick={() => { setGenForm(EMPTY_GEN); setMutError(null); setDialog('generate'); }}>
            Generate Periods
          </Button>
          <Button variant="contained" size="small" onClick={openCreate}>+ New Period</Button>
        </Stack>
      </Stack>

      <Stack spacing={2}>
        {data.data.map((period) => (
          <Card key={period.id} variant="outlined">
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box>
                  <Typography fontWeight={700}>{period.name}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {period.startDate} → {period.endDate} · Year {period.year}
                    {period.weekNumber != null && ` · Week ${period.weekNumber}`}
                    {' · '}{FREQUENCY_LABELS[period.frequency] ?? period.frequency}
                  </Typography>
                  <Chip label={STATUS_LABELS[period.status]} color={STATUS_COLORS[period.status]} size="small" sx={{ mt: 1 }} />
                </Box>
                <Stack direction="row" spacing={1} flexWrap="wrap">
                  {period.status === AssessmentPeriodStatus.Draft && (
                    <Button size="small" variant="outlined" onClick={() => openEdit(period)}>Edit</Button>
                  )}
                  {period.status === AssessmentPeriodStatus.Draft && (
                    <Button size="small" variant="outlined" color="success"
                      onClick={() => openPeriodMut.mutate(period.id)} disabled={openPeriodMut.isPending}>Open</Button>
                  )}
                  {period.status === AssessmentPeriodStatus.Open && (
                    <Button size="small" variant="outlined" color="warning"
                      onClick={() => closePeriodMut.mutate(period.id)} disabled={closePeriodMut.isPending}>Close</Button>
                  )}
                  {period.status === AssessmentPeriodStatus.Closed && (
                    <Button size="small" variant="outlined" color="info"
                      onClick={() => publishPeriodMut.mutate(period.id)} disabled={publishPeriodMut.isPending}>Publish</Button>
                  )}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>

      {data.totalPages > 1 && (
        <Stack direction="row" spacing={1} mt={2} justifyContent="center">
          <Button disabled={page <= 1} onClick={() => setPage(p => p - 1)} size="small">Prev</Button>
          <Typography variant="body2" alignSelf="center">Page {data.pageNumber} of {data.totalPages}</Typography>
          <Button disabled={!data.hasNextPage} onClick={() => setPage(p => p + 1)} size="small">Next</Button>
        </Stack>
      )}

      {/* Create / Edit Dialog */}
      <Dialog open={dialog === 'create' || dialog === 'edit'} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>{dialog === 'create' ? 'New Assessment Period' : 'Edit Period'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Name" value={form.name} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))} />
            {dialog === 'create' && (
              <FormControl fullWidth>
                <InputLabel>Frequency</InputLabel>
                <Select label="Frequency" value={form.frequency}
                  onChange={(e) => setForm(f => ({ ...f, frequency: e.target.value as AssessmentPeriodFrequency }))}>
                  <MenuItem value={AssessmentPeriodFrequency.Weekly}>Weekly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.BiWeekly}>Bi-Weekly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.Monthly}>Monthly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.Quarterly}>Quarterly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.HalfYearly}>Half-Yearly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.Annual}>Annual</MenuItem>
                </Select>
              </FormControl>
            )}
            <TextField label="Start Date (YYYY-MM-DD)" value={form.startDate} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, startDate: e.target.value }))} />
            <TextField label="End Date (YYYY-MM-DD)" value={form.endDate} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, endDate: e.target.value }))} />
            {dialog === 'create' && (
              <TextField label="Year" type="number" value={form.year} required fullWidth
                onChange={(e) => setForm(f => ({ ...f, year: +e.target.value }))} />
            )}
            <TextField label="Week Number (optional)" type="number" value={form.weekNumber} fullWidth
              onChange={(e) => setForm(f => ({ ...f, weekNumber: e.target.value }))} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button variant="contained" onClick={() => dialog === 'create' ? createMut.mutate() : updateMut.mutate()}
            disabled={isBusy || !form.name || !form.startDate || !form.endDate}>
            {isBusy ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Generate Dialog */}
      <Dialog open={dialog === 'generate'} onClose={() => setDialog(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Generate Periods</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Year" type="number" value={genForm.year} fullWidth
              onChange={(e) => setGenForm(f => ({ ...f, year: +e.target.value }))} />
            <FormControl fullWidth>
              <InputLabel>Frequency</InputLabel>
              <Select label="Frequency" value={genForm.frequency}
                onChange={(e) => setGenForm(f => ({ ...f, frequency: e.target.value as AssessmentPeriodFrequency }))}>
                <MenuItem value={AssessmentPeriodFrequency.Weekly}>Weekly</MenuItem>
                <MenuItem value={AssessmentPeriodFrequency.BiWeekly}>Bi-Weekly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.Monthly}>Monthly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.Quarterly}>Quarterly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.HalfYearly}>Half-Yearly</MenuItem>
                  <MenuItem value={AssessmentPeriodFrequency.Annual}>Annual</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button variant="contained" onClick={() => generateMut.mutate()} disabled={generateMut.isPending}>
            {generateMut.isPending ? 'Generating…' : 'Generate'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
