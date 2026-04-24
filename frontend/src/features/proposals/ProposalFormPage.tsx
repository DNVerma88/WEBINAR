import { Box, Button, FormControl, FormHelperText, InputLabel, MenuItem, Paper, Select, Stack, TextField } from '@/components/ui';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { proposalsApi } from '../../shared/api/proposals';
import { categoriesApi } from '../../shared/api/categories';
import { PageHeader } from '../../shared/components/PageHeader';
import { UnsavedChangesDialog } from '../../shared/components/UnsavedChangesDialog';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { SessionFormat, DifficultyLevel, SESSION_FORMAT_LABELS, DIFFICULTY_LEVEL_LABELS } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';

const proposalSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional().or(z.literal('')),
  categoryId: z.string().min(1, 'Category is required'),
  topic: z.string().min(1, 'Topic is required').max(200),
  format: z.nativeEnum(SessionFormat),
  difficultyLevel: z.nativeEnum(DifficultyLevel),
  estimatedDurationMinutes: z.coerce.number().min(15).max(480),
  targetAudience: z.string().max(500).optional().or(z.literal('')),
  prerequisites: z.string().max(500).optional().or(z.literal('')),
  expectedOutcomes: z.string().max(1000).optional().or(z.literal('')),
});

type ProposalFormValues = z.infer<typeof proposalSchema>;

export default function ProposalFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  usePageTitle(isEdit ? 'Edit Proposal' : 'New Proposal');
  const navigate = useNavigate();
  const qc = useQueryClient();
  const toast = useToast();

  const { data: existing } = useQuery({
    queryKey: ['proposals', id],
    queryFn: ({ signal }) => proposalsApi.getProposalById(id!, signal),
    enabled: isEdit,
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => categoriesApi.getCategories(signal),
  });

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<z.input<typeof proposalSchema>, unknown, ProposalFormValues>({
    resolver: zodResolver(proposalSchema),
    values: existing
      ? {
          title: existing.title,
          description: existing.description ?? '',
          categoryId: existing.categoryId,
          topic: existing.topic,
          format: existing.format,
          difficultyLevel: existing.difficultyLevel,
          estimatedDurationMinutes: existing.estimatedDurationMinutes,
          targetAudience: existing.targetAudience ?? '',
          prerequisites: existing.prerequisites ?? '',
          expectedOutcomes: existing.expectedOutcomes ?? '',
        }
      : undefined,
  });

  const createMutation = useMutation({
    mutationFn: proposalsApi.createProposal,
    onSuccess: (p) => { qc.invalidateQueries({ queryKey: ['proposals'] }); navigate(`/proposals/${p.id}`); },
    onError: () => toast.error('Failed to create proposal.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: ProposalFormValues) =>
      proposalsApi.updateProposal(id!, {
        title: data.title,
        description: data.description || undefined,
        categoryId: data.categoryId,
        topic: data.topic,
        format: data.format,
        difficultyLevel: data.difficultyLevel,
        estimatedDurationMinutes: data.estimatedDurationMinutes,
        targetAudience: data.targetAudience || undefined,
        prerequisites: data.prerequisites || undefined,
        expectedOutcomes: data.expectedOutcomes || undefined,
        recordVersion: existing?.recordVersion ?? 0,
      }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['proposals', id] }); navigate(`/proposals/${id}`); toast.success('Proposal updated.'); },
    onError: () => toast.error('Failed to update proposal.'),
  });

  const onSubmit = (data: ProposalFormValues) => {
    if (isEdit) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate({
        title: data.title,
        description: data.description || undefined,
        categoryId: data.categoryId,
        topic: data.topic,
        format: data.format,
        difficultyLevel: data.difficultyLevel,
        estimatedDurationMinutes: data.estimatedDurationMinutes,
        targetAudience: data.targetAudience || undefined,
        prerequisites: data.prerequisites || undefined,
        expectedOutcomes: data.expectedOutcomes || undefined,
      });
    }
  };

  const mutationError = createMutation.error ?? updateMutation.error;

  return (
    <Box>
      <UnsavedChangesDialog when={isDirty && !isSubmitting} />
      <PageHeader
        title={isEdit ? 'Edit Proposal' : 'New Proposal'}
        breadcrumbs={[
          { label: 'Proposals', to: '/proposals' },
          { label: isEdit ? 'Edit' : 'New Proposal' },
        ]}
      />

      <Paper sx={{ p: 3, maxWidth: 720 }}>
        <ApiErrorAlert error={mutationError} />

        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <TextField
            {...register('title')}
            label="Title"
            fullWidth
            autoFocus
            error={!!errors.title}
            helperText={errors.title?.message}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('topic')}
            label="Topic"
            fullWidth
            error={!!errors.topic}
            helperText={errors.topic?.message}
            sx={{ mb: 2 }}
          />

          <FormControl fullWidth sx={{ mb: 2 }} error={!!errors.categoryId}>
            <InputLabel>Category</InputLabel>
            <Controller
              name="categoryId"
              control={control}
              defaultValue=""
              render={({ field }) => (
                <Select {...field} label="Category">
                  {categories?.map((c) => (
                    <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
                  ))}
                </Select>
              )}
            />
            {errors.categoryId && <FormHelperText>{errors.categoryId.message}</FormHelperText>}
          </FormControl>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
            <FormControl fullWidth error={!!errors.format}>
              <InputLabel>Format</InputLabel>
              <Controller
                name="format"
                control={control}
                defaultValue={SessionFormat.Webinar}
                render={({ field }) => (
                  <Select {...field} label="Format">
                    {Object.values(SessionFormat).map((v) => (
                      <MenuItem key={v} value={v}>{SESSION_FORMAT_LABELS[v]}</MenuItem>
                    ))}
                  </Select>
                )}
              />
              {errors.format && <FormHelperText>{errors.format.message}</FormHelperText>}
            </FormControl>

            <FormControl fullWidth error={!!errors.difficultyLevel}>
              <InputLabel>Difficulty</InputLabel>
              <Controller
                name="difficultyLevel"
                control={control}
                defaultValue={DifficultyLevel.Beginner}
                render={({ field }) => (
                  <Select {...field} label="Difficulty">
                    {Object.values(DifficultyLevel).map((v) => (
                      <MenuItem key={v} value={v}>{DIFFICULTY_LEVEL_LABELS[v]}</MenuItem>
                    ))}
                  </Select>
                )}
              />
              {errors.difficultyLevel && <FormHelperText>{errors.difficultyLevel.message}</FormHelperText>}
            </FormControl>

            <TextField
              {...register('estimatedDurationMinutes')}
              label="Duration (min)"
              type="number"
              fullWidth
              inputProps={{ min: 15, max: 480 }}
              error={!!errors.estimatedDurationMinutes}
              helperText={errors.estimatedDurationMinutes?.message}
            />
          </Stack>

          <TextField
            {...register('description')}
            label="Description (optional)"
            multiline
            rows={4}
            fullWidth
            error={!!errors.description}
            helperText={errors.description?.message as string}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('targetAudience')}
            label="Target Audience (optional)"
            multiline
            rows={2}
            fullWidth
            error={!!errors.targetAudience}
            helperText={errors.targetAudience?.message as string}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('prerequisites')}
            label="Prerequisites (optional)"
            multiline
            rows={2}
            fullWidth
            error={!!errors.prerequisites}
            helperText={errors.prerequisites?.message as string}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('expectedOutcomes')}
            label="Expected Outcomes (optional)"
            multiline
            rows={2}
            fullWidth
            error={!!errors.expectedOutcomes}
            helperText={errors.expectedOutcomes?.message as string}
            sx={{ mb: 3 }}
          />

          <Box display="flex" gap={2}>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isEdit ? 'Save Changes' : 'Create Proposal'}
            </Button>
            <Button onClick={() => navigate(-1)}>Cancel</Button>
          </Box>
        </Box>
      </Paper>
    </Box>
  );
}
