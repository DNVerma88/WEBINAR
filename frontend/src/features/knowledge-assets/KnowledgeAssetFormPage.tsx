import {
  Box,
  Button,
  Checkbox,
  FormControl,
  FormControlLabel,
  FormHelperText,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
} from '@/components/ui';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { knowledgeAssetsApi } from '../../shared/api/knowledgeAssets';
import { PageHeader } from '../../shared/components/PageHeader';
import { UnsavedChangesDialog } from '../../shared/components/UnsavedChangesDialog';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { KnowledgeAssetType, KnowledgeAssetTypeLabel } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  url: z.string().min(1, 'URL is required').url('Must be a valid URL'),
  description: z.string().max(2000).optional().or(z.literal('')),
  assetType: z.nativeEnum(KnowledgeAssetType),
  isPublic: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

export default function KnowledgeAssetFormPage() {
  usePageTitle('Upload Knowledge Asset');
  const navigate = useNavigate();
  const qc = useQueryClient();
  const toast = useToast();

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { assetType: KnowledgeAssetType.Recording, isPublic: true },
  });

  const createMutation = useMutation({
    mutationFn: (data: FormValues) =>
      knowledgeAssetsApi.createAsset({
        title: data.title,
        url: data.url,
        description: data.description || undefined,
        assetType: data.assetType,
        isPublic: data.isPublic,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-assets'] });
      navigate('/knowledge-assets');
      toast.success('Asset uploaded successfully!');
    },
    onError: () => toast.error('Failed to upload asset.'),
  });

  return (
    <Box>
      <UnsavedChangesDialog when={isDirty && !isSubmitting} />
      <PageHeader
        title="Upload Knowledge Asset"
        breadcrumbs={[
          { label: 'Knowledge Assets', to: '/knowledge-assets' },
          { label: 'Upload Asset' },
        ]}
      />

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <ApiErrorAlert error={createMutation.error} />

        <Box component="form" onSubmit={handleSubmit((d) => createMutation.mutate(d))} noValidate>
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
              {...register('url')}
              label="URL"
              fullWidth
              required
              placeholder="https://..."
              error={!!errors.url}
              helperText={errors.url?.message}
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

            <Controller
              name="assetType"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth error={!!errors.assetType}>
                  <InputLabel>Asset Type</InputLabel>
                  <Select {...field} label="Asset Type">
                    {Object.values(KnowledgeAssetType).map((v) => (
                      <MenuItem key={v} value={v}>{KnowledgeAssetTypeLabel[v] ?? v}</MenuItem>
                    ))}
                  </Select>
                  {errors.assetType && (
                    <FormHelperText>{errors.assetType.message as string}</FormHelperText>
                  )}
                </FormControl>
              )}
            />

            <Controller
              name="isPublic"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  control={<Checkbox checked={field.value} onChange={field.onChange} />}
                  label="Make this asset publicly visible"
                />
              )}
            />

            <Stack direction="row" spacing={2} pt={1}>
              <Button type="submit" variant="contained" disabled={isSubmitting}>
                Upload Asset
              </Button>
              <Button onClick={() => navigate(-1)}>Cancel</Button>
            </Stack>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
