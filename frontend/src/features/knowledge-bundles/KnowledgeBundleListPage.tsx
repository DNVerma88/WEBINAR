import {
  AddIcon,
  Box,
  BundleIcon,
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
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { knowledgeBundlesApi } from '../../shared/api/knowledgeBundles';
import { categoriesApi } from '../../shared/api/categories';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';
import { UserRole } from '../../shared/types';
import type { CreateKnowledgeBundleRequest } from '../../shared/types';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const createSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(1000).optional().or(z.literal('')),
  categoryId: z.string().optional(),
  coverImageUrl: z.string().url('Must be a valid URL').optional().or(z.literal('')),
});
type CreateFormValues = z.infer<typeof createSchema>;

export default function KnowledgeBundleListPage() {
  usePageTitle('Knowledge Bundles');
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { hasRole } = useAuth();

  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ['knowledge-bundles', search, categoryId, page],
    queryFn: ({ signal }) =>
      knowledgeBundlesApi.getBundles({
        searchTerm: search || undefined,
        categoryId: categoryId || undefined,
        pageNumber: page,
        pageSize: 15,
      }, signal),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => categoriesApi.getCategories(signal),
  });

  const toast = useToast();
  const isAdmin = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);
  const canCreate = isAdmin || hasRole(UserRole.Contributor) || hasRole(UserRole.KnowledgeTeam);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateFormValues>({ resolver: zodResolver(createSchema) });

  const createMutation = useMutation({
    mutationFn: (data: CreateKnowledgeBundleRequest) => knowledgeBundlesApi.createBundle(data),
    onSuccess: (bundle) => {
      qc.invalidateQueries({ queryKey: ['knowledge-bundles'] });
      setShowCreate(false);
      reset();
      toast.success('Bundle created successfully.');
      navigate(`/knowledge-bundles/${bundle.id}`);
    },
    onError: () => toast.error('Failed to create bundle.'),
  });

  const onSubmit = (data: CreateFormValues) => {
    createMutation.mutate({
      title: data.title,
      description: data.description || undefined,
      categoryId: data.categoryId || undefined,
      coverImageUrl: data.coverImageUrl || undefined,
    });
  };

  return (
    <Box>
      <PageHeader
        title="Knowledge Bundles"
        subtitle="Curated collections of learning resources"
        actions={
          canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowCreate(true)}>
              Create Bundle
            </Button>
          )
        }
      />

      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search bundles"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 280 }}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel>Category</InputLabel>
          <Select value={categoryId} label="Category" onChange={(e) => { setCategoryId(e.target.value); setPage(1); }}>
            <MenuItem value="">All Categories</MenuItem>
            {(categories ?? []).map((c) => (
              <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      <ApiErrorAlert error={error} />

      {isLoading ? (
        <LoadingOverlay />
      ) : (
        <>
          <Stack spacing={2}>
            {data?.data.map((bundle) => (
              <Card
                key={bundle.id}
                sx={{ cursor: 'pointer', '&:hover': { boxShadow: 4 } }}
                onClick={() => navigate(`/knowledge-bundles/${bundle.id}`)}
              >
                <CardContent>
                  <Box display="flex" justifyContent="space-between" alignItems="flex-start">
                    <Box display="flex" gap={2} flex={1}>
                      <BundleIcon color="primary" sx={{ mt: 0.5, flexShrink: 0 }} />
                      <Box flex={1}>
                        <Box display="flex" alignItems="center" gap={1} mb={0.5} flexWrap="wrap">
                          <Typography variant="subtitle1" fontWeight={600}>{bundle.title}</Typography>
                          {bundle.isPublished ? (
                            <Chip label="Published" size="small" color="success" />
                          ) : (
                            <Chip label="Draft" size="small" color="default" />
                          )}
                          {bundle.categoryName && (
                            <Chip label={bundle.categoryName} size="small" variant="outlined" />
                          )}
                        </Box>
                        {bundle.description && (
                          <Typography variant="body2" color="text.secondary" mb={0.5}>
                            {bundle.description}
                          </Typography>
                        )}
                        <Typography variant="caption" color="text.secondary">
                          By {bundle.createdByName} · {bundle.itemCount} item{bundle.itemCount !== 1 ? 's' : ''} ·{' '}
                          {new Date(bundle.createdDate).toLocaleDateString()}
                        </Typography>
                      </Box>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            ))}
          </Stack>

          {data && data.data.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={6}>
              No knowledge bundles found.
            </Typography>
          )}

          {data && data.totalPages > 1 && (
            <Box display="flex" justifyContent="center" mt={4} gap={1}>
              <Button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <Chip label={`${page} / ${data.totalPages}`} />
              <Button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </Box>
          )}
        </>
      )}

      {/* Create Bundle Dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogTitle>Create Knowledge Bundle</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={createMutation.error} />
            <TextField
              {...register('title')}
              label="Bundle Title"
              fullWidth
              autoFocus
              error={!!errors.title}
              helperText={errors.title?.message}
              sx={{ mt: 1, mb: 2 }}
            />
            <TextField
              {...register('description')}
              label="Description (optional)"
              multiline
              rows={3}
              fullWidth
              sx={{ mb: 2 }}
            />
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Category (optional)</InputLabel>
              <Select
                defaultValue=""
                label="Category (optional)"
                inputProps={register('categoryId')}
              >
                <MenuItem value="">None</MenuItem>
                {(categories ?? []).map((c) => (
                  <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              {...register('coverImageUrl')}
              label="Cover Image URL (optional)"
              fullWidth
              error={!!errors.coverImageUrl}
              helperText={errors.coverImageUrl?.message as string}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowCreate(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting || createMutation.isPending}>
              Create
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
}
