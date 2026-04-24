import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Stack,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { assessmentPeriodApi, assessmentGroupApi, employeeAssessmentApi, ratingScaleApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { AssessmentStatus, AssessmentPeriodStatus } from '../types';
import type { EmployeeAssessmentDto } from '../types';
import { useToast } from '../../../shared/hooks/useToast';

const STATUS_LABEL: Record<AssessmentStatus, string> = {
  [AssessmentStatus.Draft]:     'Draft',
  [AssessmentStatus.Submitted]: 'Submitted',
  [AssessmentStatus.Reopened]:  'Reopened',
};

const STATUS_COLOR: Record<AssessmentStatus, 'default' | 'success' | 'warning'> = {
  [AssessmentStatus.Draft]:     'default',
  [AssessmentStatus.Submitted]: 'success',
  [AssessmentStatus.Reopened]:  'warning',
};

export function AssessmentGridTab() {
  const toast = useToast();
  const [groupId, setGroupId] = useState('');
  const [periodId, setPeriodId] = useState('');
  const [assessments, setAssessments] = useState<EmployeeAssessmentDto[]>([]);
  const [dirtyRatings, setDirtyRatings] = useState<Record<string, { ratingScaleId: string; ratingValue: number; comment: string }>>({});

  const { data: periods } = useQuery({
    queryKey: ['assessment', 'periods-list', 'open'],
    queryFn: () => assessmentPeriodApi.getPeriods({ pageNumber: 1, pageSize: 100, status: AssessmentPeriodStatus.Open }),
    select: (d) => d.data,
  });

  const { data: groups } = useQuery({
    queryKey: ['assessment', 'groups-list'],
    queryFn: () => assessmentGroupApi.getGroups({ pageNumber: 1, pageSize: 100, isActive: true }),
    select: (d) => d.data,
  });

  const { data: scales } = useQuery({
    queryKey: ['assessment', 'scales'],
    queryFn: ratingScaleApi.getScales,
  });

  const gridMut = useMutation({
    mutationFn: () => employeeAssessmentApi.getOrCreateDrafts({ groupId, periodId }),
    onSuccess: (data) => setAssessments(data),
  });

  const bulkSaveMut = useMutation({
    mutationFn: () => employeeAssessmentApi.bulkSave({
      assessmentPeriodId: periodId,
      groupId,
      assessments: Object.entries(dirtyRatings).map(([userId, vals]) => ({
        userId,
        groupId,
        assessmentPeriodId: periodId,
        ...vals,
      })),
    }),
    onSuccess: (data) => {
      setAssessments(data);
      setDirtyRatings({});
      toast.success('Assessments saved.');
    },
    onError: () => toast.error('Failed to save assessments.'),
  });

  const bulkSubmitMut = useMutation({
    mutationFn: () => employeeAssessmentApi.bulkSubmit({ groupId, periodId }),
    onSuccess: () => { gridMut.mutate(); toast.success('Assessments submitted.'); },
    onError: () => toast.error('Failed to submit assessments.'),
  });

  const updateDirty = (userId: string, field: string, value: string | number) => {
    const existing = assessments.find(a => a.userId === userId);
    const defaults = {
      ratingScaleId: existing?.ratingScaleId ?? '',
      ratingValue:   existing?.ratingValue ?? 0,
      comment:       existing?.comment ?? '',
    };
    setDirtyRatings(prev => ({
      ...prev,
      [userId]: { ...defaults, ...prev[userId], [field]: value },
    }));
  };

  return (
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" mb={2}>Load Assessment Grid</Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <Box component="select" sx={{ p: 1, borderRadius: 1, border: '1px solid', borderColor: 'divider', flex: 1 }}
              value={groupId} onChange={(e) => setGroupId((e.target as HTMLSelectElement).value)}>
              <option value="">-- Select Group --</option>
              {groups?.map(g => <option key={g.id} value={g.id}>{g.groupName}</option>)}
            </Box>
            <Box component="select" sx={{ p: 1, borderRadius: 1, border: '1px solid', borderColor: 'divider', flex: 1 }}
              value={periodId} onChange={(e) => setPeriodId((e.target as HTMLSelectElement).value)}>
              <option value="">-- Select Period --</option>
              {periods?.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </Box>
            <Button
              variant="contained"
              onClick={() => gridMut.mutate()}
              disabled={!groupId || !periodId || gridMut.isPending}
            >
              Load Grid
            </Button>
          </Stack>
        </CardContent>
      </Card>

      {gridMut.isError && <ApiErrorAlert error={gridMut.error} />}
      {gridMut.isPending && <LoadingOverlay />}

      {assessments.length > 0 && (
        <>
          <Stack direction="row" justifyContent="flex-end" spacing={2} mb={2}>
            <Button
              variant="outlined"
              onClick={() => bulkSaveMut.mutate()}
              disabled={Object.keys(dirtyRatings).length === 0 || bulkSaveMut.isPending}
            >
              Save All
            </Button>
            <Button
              variant="contained"
              color="success"
              onClick={() => bulkSubmitMut.mutate()}
              disabled={bulkSubmitMut.isPending}
            >
              Submit All
            </Button>
          </Stack>

          <Stack spacing={1}>
            {assessments.map((a) => {
              const dirty = dirtyRatings[a.userId];
              const currentScaleId  = dirty?.ratingScaleId   ?? a.ratingScaleId;
              const currentComment  = dirty?.comment          ?? a.comment ?? '';

              return (
                <Card key={a.id} variant="outlined"
                  sx={{ borderLeft: 4, borderLeftColor: a.status === AssessmentStatus.Submitted ? 'success.main' : 'warning.main' }}>
                  <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="center">
                      <Box sx={{ flex: 2 }}>
                        <Typography fontWeight={600}>{a.employeeName}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {a.designation ?? 'N/A'} · {a.department ?? 'N/A'}
                        </Typography>
                      </Box>

                      <Box component="select"
                        sx={{ p: 0.8, borderRadius: 1, border: '1px solid', borderColor: 'divider', flex: 1 }}
                        value={currentScaleId}
                        disabled={a.status === AssessmentStatus.Submitted}
                        onChange={(e) => updateDirty(a.userId, 'ratingScaleId', (e.target as HTMLSelectElement).value)}>
                        <option value="">-- Rating --</option>
                        {scales?.filter(s => s.isActive).map(s => (
                          <option key={s.id} value={s.id}>{s.numericValue} – {s.name}</option>
                        ))}
                      </Box>

                      <Box sx={{ flex: 2 }}>
                        <input
                          style={{ width: '100%', padding: '6px 8px', borderRadius: 4, border: '1px solid #ccc' }}
                          placeholder="Comment (optional)"
                          value={currentComment}
                          disabled={a.status === AssessmentStatus.Submitted}
                          onChange={(e) => updateDirty(a.userId, 'comment', e.target.value)}
                        />
                      </Box>

                      <Chip
                        label={STATUS_LABEL[a.status]}
                        color={STATUS_COLOR[a.status]}
                        size="small"
                      />
                    </Stack>
                  </CardContent>
                </Card>
              );
            })}
          </Stack>
        </>
      )}
    </Box>
  );
}
