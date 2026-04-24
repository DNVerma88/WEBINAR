import {
  Box,
  Button,
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
} from '@/components/ui';
import { useForm, Controller } from 'react-hook-form';
import type { SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { learningPathsApi } from '../../shared/api/learningPaths';
import { categoriesApi } from '../../shared/api/categories';
import { PageHeader } from '../../shared/components/PageHeader';
import { UnsavedChangesDialog } from '../../shared/components/UnsavedChangesDialog';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { DifficultyLevel, DIFFICULTY_LEVEL_LABELS } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional().or(z.literal('')),
  objective: z.string().max(500).optional().or(z.literal('')),
  difficultyLevel: z.nativeEnum(DifficultyLevel),
  estimatedDurationMinutes: z.number().int().min(0).max(10000),
  categoryId: z.string().optional().or(z.literal('')),
});

type FormValues = z.infer<typeof schema>;

export default function LearningPathFormPage() {
  const toast = useToast();
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  usePageTitle(isEdit ? 'Edit Learning Path' : 'New Learning Path');
  const navigate = useNavigate();
  const qc = useQueryClient();

  const { data: existing } = useQuery({
    queryKey: ['learning-paths', id],
    queryFn: ({ signal }) => learningPathsApi.getPathById(id!, signal),
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
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      objective: '',
      difficultyLevel: DifficultyLevel.Beginner,
      estimatedDurationMinutes: 60,
      categoryId: '',
    },
    values: existing
      ? {
          title: existing.title,
          description: existing.description ?? '',
          objective: existing.objective ?? '',
          difficultyLevel: existing.difficultyLevel,
          estimatedDurationMinutes: existing.estimatedDurationMinutes,
          categoryId: existing.categoryId ?? '',
        }
      : undefined,
  });

  const createMutation = useMutation({
    mutationFn: (data: FormValues) =>
      learningPathsApi.createPath({
        title: data.title,
        description: data.description || undefined,
        objective: data.objective || undefined,
        difficultyLevel: data.difficultyLevel,
        estimatedDurationMinutes: data.estimatedDurationMinutes || undefined,
        categoryId: data.categoryId || undefined,
      }),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: ['learning-paths'] });
      toast.success('Learning path created.');
      navigate(`/learning-paths/${result.id}`);
    },
    onError: () => toast.error('Failed to create learning path.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: FormValues) =>
      learningPathsApi.updatePath(id!, {
        title: data.title,
        description: data.description || undefined,
        objective: data.objective || undefined,
        isPublished: existing?.isPublished ?? false,
        isAssignable: existing?.isAssignable ?? false,
        estimatedDurationMinutes: data.estimatedDurationMinutes || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['learning-paths', id] });
      qc.invalidateQueries({ queryKey: ['learning-paths'] });
      toast.success('Learning path updated.');
      navigate(`/learning-paths/${id}`);
    },
    onError: () => toast.error('Failed to update learning path.'),
  });

  const onSubmit: SubmitHandler<FormValues> = (data) => {
    if (isEdit) updateMutation.mutate(data);
    else createMutation.mutate(data);
  };

  const mutationError = createMutation.error ?? updateMutation.error;

  return (
    <Box>
      <UnsavedChangesDialog when={isDirty && !isSubmitting} />
      <PageHeader
        title={isEdit ? 'Edit Learning Path' : 'New Learning Path'}
        breadcrumbs={[
          { label: 'Learning Paths', to: '/learning-paths' },
          { label: isEdit ? 'Edit' : 'New Learning Path' },
        ]}
      />

      <Paper sx={{ p: 3, maxWidth: 640 }}>
        <ApiErrorAlert error={mutationError} />

        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            <TextField
              {...register('title')}
              label="Title"
              fullWidth
              required
              error={!!errors.title}
              helperText={errors.title?.message}
            />

            <TextField
              {...register('description')}
              label="Description"
              fullWidth
              multiline
              rows={3}
              error={!!errors.description}
              helperText={errors.description?.message as string}
            />

            <TextField
              {...register('objective')}
              label="Learning Objective"
              fullWidth
              multiline
              rows={2}
              error={!!errors.objective}
              helperText={errors.objective?.message as string}
            />

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <Controller
                name="difficultyLevel"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth error={!!errors.difficultyLevel}>
                    <InputLabel>Difficulty</InputLabel>
                    <Select {...field} label="Difficulty">
                      {Object.values(DifficultyLevel).map((v) => (
                        <MenuItem key={v} value={v}>{DIFFICULTY_LEVEL_LABELS[v]}</MenuItem>
                      ))}
                    </Select>
                    {errors.difficultyLevel && (
                      <FormHelperText>{errors.difficultyLevel.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />

              <TextField
                {...register('estimatedDurationMinutes')}
                label="Estimated Duration (minutes)"
                type="number"
                fullWidth
                inputProps={{ min: 0 }}
                error={!!errors.estimatedDurationMinutes}
                helperText={errors.estimatedDurationMinutes?.message as string}
              />
            </Stack>

            <Controller
              name="categoryId"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth>
                  <InputLabel>Category (optional)</InputLabel>
                  <Select {...field} label="Category (optional)">
                    <MenuItem value="">None</MenuItem>
                    {categories?.map((c) => (
                      <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            />

            <Stack direction="row" spacing={2} pt={1}>
              <Button type="submit" variant="contained" disabled={isSubmitting}>
                {isEdit ? 'Save Changes' : 'Create Learning Path'}
              </Button>
              <Button onClick={() => navigate(-1)}>Cancel</Button>
            </Stack>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
