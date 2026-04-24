import { Box, Button, Chip, FormControl, FormHelperText, InputLabel, MenuItem, Paper, Select, TextField } from '@/components/ui';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { sessionsApi } from '../../shared/api/sessions';
import { proposalsApi } from '../../shared/api/proposals';
import { tagsApi } from '../../shared/api/tags';
import { speakersApi } from '../../shared/api/speakers';
import { PageHeader } from '../../shared/components/PageHeader';
import { UnsavedChangesDialog } from '../../shared/components/UnsavedChangesDialog';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { MeetingPlatform, ProposalStatus, MEETING_PLATFORM_LABELS } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';

const sessionSchema = z.object({
  proposalId: z.string().min(1, 'Proposal is required'),
  speakerId: z.string().optional(),
  scheduledAt: z.string().min(1, 'Scheduled date is required'),
  durationMinutes: z.coerce.number().min(15).max(480),
  meetingLink: z
    .string()
    .min(1, 'Meeting link is required')
    .refine(
      (val) => {
        try { new URL(val); return true; } catch { return false; }
      },
      { message: 'Must be a valid URL' },
    ),
  meetingPlatform: z.nativeEnum(MeetingPlatform).optional().or(z.literal('')),
  participantLimit: z.coerce.number().min(1).optional().or(z.literal('')),
  tagIds: z.array(z.string()).optional(),
});

type SessionFormValues = z.infer<typeof sessionSchema>;

export default function SessionFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  usePageTitle(isEdit ? 'Edit Session' : 'Schedule Session');
  const navigate = useNavigate();
  const qc = useQueryClient();
  const toast = useToast();

  const { data: existing } = useQuery({
    queryKey: ['sessions', id],
    queryFn: ({ signal }) => sessionsApi.getSessionById(id!, signal),
    enabled: isEdit,
  });

  const { data: publishedProposals } = useQuery({
    queryKey: ['proposals', 'published'],
    queryFn: ({ signal }) => proposalsApi.getProposals({ status: ProposalStatus.Published, pageSize: 100 }, signal),
    enabled: !isEdit,
    staleTime: 0,
  });

  const { data: allTags } = useQuery({
    queryKey: ['tags'],
    queryFn: ({ signal }) => tagsApi.getTags({ pageSize: 200 }, signal),
  });
  const tagOptions = allTags?.data ?? [];

  const { data: speakersList } = useQuery({
    queryKey: ['speakers', 'all'],
    queryFn: ({ signal }) => speakersApi.getSpeakers({ pageSize: 200 }, signal),
  });
  const speakerOptions = speakersList?.data ?? [];

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isDirty },
  } = useForm<z.input<typeof sessionSchema>, unknown, SessionFormValues>({
    resolver: zodResolver(sessionSchema),
    values: existing
      ? {
          proposalId: existing.proposalId,
          speakerId: existing.speakerId,
          scheduledAt: existing.scheduledAt
            ? new Date(existing.scheduledAt).toISOString().slice(0, 16)
            : '',
          durationMinutes: existing.durationMinutes,
          meetingLink: existing.meetingLink ?? '',
          meetingPlatform: existing.meetingPlatform,
          participantLimit: existing.participantLimit ?? '',
          tagIds: existing.tags
            .map(tagName => tagOptions.find(t => t.name === tagName)?.id ?? '')
            .filter(Boolean),
        }
      : undefined,
  });

  const createMutation = useMutation({
    mutationFn: sessionsApi.createSession,
    onSuccess: (s) => { qc.invalidateQueries({ queryKey: ['sessions'] }); navigate(`/sessions/${s.id}`); },
    onError: () => toast.error('Failed to schedule session.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: SessionFormValues) =>
      sessionsApi.updateSession(id!, {
        scheduledAt: data.scheduledAt,
        durationMinutes: data.durationMinutes,
        meetingLink: data.meetingLink,
        meetingPlatform: data.meetingPlatform as MeetingPlatform,
        participantLimit: data.participantLimit ? Number(data.participantLimit) : undefined,
        tagIds: data.tagIds ?? [],
        speakerId: data.speakerId || undefined,
        recordVersion: existing!.recordVersion,
      }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['sessions', id] }); navigate(`/sessions/${id}`); toast.success('Session updated.'); },
    onError: () => toast.error('Failed to update session.'),
  });

  const onSubmit = (data: SessionFormValues) => {
    if (isEdit) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate({
        proposalId: data.proposalId,
        speakerId: data.speakerId || undefined,
        scheduledAt: data.scheduledAt,
        durationMinutes: data.durationMinutes,
        meetingLink: data.meetingLink,
        meetingPlatform: data.meetingPlatform as MeetingPlatform,
        participantLimit: data.participantLimit ? Number(data.participantLimit) : undefined,
        tagIds: data.tagIds ?? [],
      });
    }
  };

  const mutationError = createMutation.error ?? updateMutation.error;
  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <UnsavedChangesDialog when={isDirty && !isPending} />
      <PageHeader
        title={isEdit ? 'Edit Session' : 'Schedule Session'}
        breadcrumbs={[
          { label: 'Sessions', to: '/sessions' },
          { label: isEdit ? 'Edit' : 'New Session' },
        ]}
      />

      <Paper sx={{ p: 3, maxWidth: 640 }}>
        <ApiErrorAlert error={mutationError} />

        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          {!isEdit && (
            <FormControl fullWidth sx={{ mb: 2 }} error={!!errors.proposalId}>
              <InputLabel>Approved Proposal</InputLabel>
              <Controller
                name="proposalId"
                control={control}
                defaultValue=""
                render={({ field }) => (
                  <Select {...field} label="Approved Proposal">
                    {publishedProposals?.data.map((p) => (
                      <MenuItem key={p.id} value={p.id}>{p.title}</MenuItem>
                    ))}
                  </Select>
                )}
              />
              {errors.proposalId && <FormHelperText>{errors.proposalId.message}</FormHelperText>}
            </FormControl>
          )}

          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>Speaker (defaults to proposal author)</InputLabel>
            <Controller
              name="speakerId"
              control={control}
              defaultValue=""
              render={({ field }) => (
                <Select {...field} label="Speaker (defaults to proposal author)" value={field.value ?? ''}>
                  <MenuItem value=""><em>Use proposal author</em></MenuItem>
                  {speakerOptions.map((s) => (
                    <MenuItem key={s.userId} value={s.userId}>{s.fullName}</MenuItem>
                  ))}
                </Select>
              )}
            />
          </FormControl>

          <TextField
            {...register('scheduledAt')}
            label="Scheduled At"
            type="datetime-local"
            fullWidth
            InputLabelProps={{ shrink: true }}
            error={!!errors.scheduledAt}
            helperText={errors.scheduledAt?.message}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('durationMinutes')}
            label="Duration (minutes)"
            type="number"
            fullWidth
            inputProps={{ min: 15, max: 480 }}
            error={!!errors.durationMinutes}
            helperText={errors.durationMinutes?.message}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('meetingLink')}
            label="Meeting Link"
            fullWidth
            error={!!errors.meetingLink}
            helperText={errors.meetingLink?.message}
            sx={{ mb: 2 }}
          />

          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>Meeting Platform (optional)</InputLabel>
            <Controller
              name="meetingPlatform"
              control={control}
              defaultValue={undefined}
              render={({ field }) => (
                <Select {...field} label="Meeting Platform (optional)" value={field.value ?? ''}>
                  <MenuItem value="">None</MenuItem>
                  {Object.values(MeetingPlatform).map((v) => (
                      <MenuItem key={v} value={v}>{MEETING_PLATFORM_LABELS[v]}</MenuItem>
                    ))}
                </Select>
              )}
            />
          </FormControl>

          <TextField
            {...register('participantLimit')}
            label="Participant Limit (optional)"
            type="number"
            fullWidth
            inputProps={{ min: 1 }}
            error={!!errors.participantLimit}
            helperText={errors.participantLimit?.message as string}
            sx={{ mb: 2 }}
          />

          <FormControl fullWidth sx={{ mb: 3 }}>
            <InputLabel>Tags (optional)</InputLabel>
            <Controller
              name="tagIds"
              control={control}
              defaultValue={[]}
              render={({ field }) => (
                <Select
                  {...field}
                  multiple
                  label="Tags (optional)"
                  value={field.value ?? []}
                  renderValue={(selected) => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {(selected as string[]).map((v) => (
                        <Chip key={v} label={tagOptions.find(t => t.id === v)?.name ?? v} size="small" />
                      ))}
                    </Box>
                  )}
                >
                  {tagOptions.map((tag) => (
                    <MenuItem key={tag.id} value={tag.id}>{tag.name}</MenuItem>
                  ))}
                </Select>
              )}
            />
          </FormControl>

          <Box display="flex" gap={2}>
            <Button type="submit" variant="contained" disabled={isPending}>
              {isEdit ? 'Save Changes' : 'Schedule'}
            </Button>
            <Button onClick={() => navigate(-1)}>Cancel</Button>
          </Box>
        </Box>
      </Paper>
    </Box>
  );
}
