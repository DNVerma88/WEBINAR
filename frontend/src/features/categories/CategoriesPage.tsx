import {
  AddIcon,
  Box,
  Button,
  Chip,
  DeleteIcon,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  EditIcon,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { categoriesApi } from '../../shared/api/categories';
import type { CategoryDto } from '../../shared/types';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';

const categorySchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  description: z.string().max(500).optional().or(z.literal('')),
  sortOrder: z.coerce.number().min(0).optional(),
});

type CategoryFormValues = z.infer<typeof categorySchema>;

export default function CategoriesPage() {
  usePageTitle('Categories');
  const qc = useQueryClient();
  const toast = useToast();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<CategoryDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<CategoryDto | null>(null);

  const { data: categories, isLoading, error } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => categoriesApi.getCategories(signal),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<z.input<typeof categorySchema>, unknown, CategoryFormValues>({
    resolver: zodResolver(categorySchema),
  });

  const openCreate = () => {
    setEditing(null);
    reset({ name: '', description: '', sortOrder: 0 });
    setDialogOpen(true);
  };

  const openEdit = (cat: CategoryDto) => {
    setEditing(cat);
    reset({ name: cat.name, description: cat.description ?? '', sortOrder: cat.sortOrder });
    setDialogOpen(true);
  };

  const createMutation = useMutation({
    mutationFn: (data: CategoryFormValues) =>
      categoriesApi.createCategory({ name: data.name, description: data.description || undefined, sortOrder: data.sortOrder ?? 0 }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['categories'] }); setDialogOpen(false); toast.success('Category created.'); },
    onError: () => toast.error('Failed to create category.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: CategoryFormValues) =>
      categoriesApi.updateCategory(editing!.id, {
        name: data.name,
        description: data.description || undefined,
        sortOrder: data.sortOrder ?? editing!.sortOrder,
        isActive: editing!.isActive,
        recordVersion: editing!.recordVersion,
      }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['categories'] }); setDialogOpen(false); toast.success('Category updated.'); },
    onError: () => toast.error('Failed to update category.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => categoriesApi.deleteCategory(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['categories'] }); setDeleteTarget(null); toast.success('Category deleted.'); },
    onError: () => toast.error('Failed to delete category.'),
  });

  const onSubmit = (data: CategoryFormValues) => {
    if (editing) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
  };

  const mutationError = createMutation.error ?? updateMutation.error ?? deleteMutation.error;

  if (isLoading) return <LoadingOverlay />;

  return (
    <Box>
      <PageHeader
        title="Categories"
        breadcrumbs={[{ label: 'Categories' }]}
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
            New Category
          </Button>
        }
      />

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={mutationError} />

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Description</TableCell>
              <TableCell align="center">Sort Order</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {categories?.map((cat) => (
              <TableRow key={cat.id} hover>
                <TableCell><Typography variant="body2" fontWeight={500}>{cat.name}</Typography></TableCell>
                <TableCell><Typography variant="body2" color="text.secondary">{cat.description ?? '—'}</Typography></TableCell>
                <TableCell align="center">{cat.sortOrder}</TableCell>
                <TableCell align="center">
                  <Chip label={cat.isActive ? 'Active' : 'Inactive'} color={cat.isActive ? 'success' : 'default'} size="small" />
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                    <Tooltip title="Edit">
                      <IconButton size="small" onClick={() => openEdit(cat)}><EditIcon fontSize="small" /></IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton size="small" color="error" onClick={() => setDeleteTarget(cat)}><DeleteIcon fontSize="small" /></IconButton>
                    </Tooltip>
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
            {!categories?.length && (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <Typography variant="body2" color="text.secondary" py={3}>No categories yet.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Create / Edit Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogTitle>{editing ? 'Edit Category' : 'New Category'}</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={createMutation.error ?? updateMutation.error} />
            <TextField {...register('name')} label="Name" fullWidth
              error={!!errors.name} helperText={errors.name?.message} sx={{ mt: 1, mb: 2 }} />
            <TextField {...register('description')} label="Description (optional)" fullWidth multiline rows={2}
              error={!!errors.description} helperText={errors.description?.message as string} sx={{ mb: 2 }} />
            <TextField {...register('sortOrder')} label="Sort Order" type="number" fullWidth
              error={!!errors.sortOrder} helperText={errors.sortOrder?.message as string}
              inputProps={{ min: 0 }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {editing ? 'Save Changes' : 'Create'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Category"
        message={`Delete category "${deleteTarget?.name}"? This will deactivate it.`}
        confirmLabel="Delete"
        onConfirm={() => deleteMutation.mutate(deleteTarget!.id)}
        onCancel={() => setDeleteTarget(null)}
        danger
      />
    </Box>
  );
}
