import { Box, Button, Paper, Stack, TextField } from '@/components/ui';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { communitiesApi } from '../../shared/api/communities';
import { PageHeader } from '../../shared/components/PageHeader';
import { UnsavedChangesDialog } from '../../shared/components/UnsavedChangesDialog';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';

const communitySchema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  description: z.string().max(2000).optional().or(z.literal('')),
});

type CommunityFormValues = z.infer<typeof communitySchema>;

export default function CommunityFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  usePageTitle(isEdit ? 'Edit Community' : 'New Community');
  const navigate = useNavigate();
  const qc = useQueryClient();
  const toast = useToast();

  const { data: existing } = useQuery({
    queryKey: ['communities', id],
    queryFn: ({ signal }) => communitiesApi.getCommunityById(id!, signal),
    enabled: isEdit,
  });

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<CommunityFormValues>({
    resolver: zodResolver(communitySchema),
    values: existing
      ? {
          name: existing.name,
          description: existing.description ?? '',
        }
      : undefined,
  });

  const createMutation = useMutation({
    mutationFn: communitiesApi.createCommunity,
    onSuccess: (c) => {
      qc.invalidateQueries({ queryKey: ['communities'] });
      navigate(`/communities/${c.id}`);
    },
    onError: () => toast.error('Failed to create community.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: CommunityFormValues) =>
      communitiesApi.updateCommunity(id!, { name: data.name, description: data.description || undefined }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['communities', id] });
      navigate(`/communities/${id}`);
      toast.success('Community updated.');
    },
    onError: () => toast.error('Failed to update community.'),
  });

  const onSubmit = (data: CommunityFormValues) => {
    if (isEdit) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate({ name: data.name, description: data.description || undefined });
    }
  };

  const mutationError = createMutation.error ?? updateMutation.error;

  return (
    <Box>
      <UnsavedChangesDialog when={isDirty && !isSubmitting} />
      <PageHeader
        title={isEdit ? 'Edit Community' : 'New Community'}
        breadcrumbs={[
          { label: 'Communities', to: '/communities' },
          { label: isEdit ? 'Edit' : 'New Community' },
        ]}
      />

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <ApiErrorAlert error={mutationError} />

        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <TextField
            {...register('name')}
            label="Community Name"
            fullWidth
            error={!!errors.name}
            helperText={errors.name?.message}
            sx={{ mb: 2 }}
          />

          <TextField
            {...register('description')}
            label="Description (optional)"
            fullWidth
            multiline
            rows={4}
            error={!!errors.description}
            helperText={errors.description?.message as string}
            sx={{ mb: 3 }}
          />

          <Stack direction="row" spacing={2}>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isEdit ? 'Save Changes' : 'Create Community'}
            </Button>
            <Button onClick={() => navigate(-1)}>Cancel</Button>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
