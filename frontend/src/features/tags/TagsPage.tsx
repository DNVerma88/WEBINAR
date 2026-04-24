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
  IconButton,
  InputAdornment,
  Paper,
  SearchIcon,
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
import { tagsApi } from '../../shared/api/tags';
import type { TagDto } from '../../shared/types';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';

const tagSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
});

type TagFormValues = z.infer<typeof tagSchema>;

export default function TagsPage() {
  const toast = useToast();
  usePageTitle('Tags');
  const qc = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<TagDto | null>(null);
  const [search, setSearch] = useState('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['tags', search],
    queryFn: ({ signal }) => tagsApi.getTags({ search: search || undefined, pageSize: 100 }, signal),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TagFormValues>({ resolver: zodResolver(tagSchema) });

  const createMutation = useMutation({
    mutationFn: (data: TagFormValues) => tagsApi.createTag({ name: data.name }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tags'] });
      setDialogOpen(false);
      reset();
      toast.success('Tag created.');
    },
    onError: () => toast.error('Failed to create tag.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => tagsApi.deleteTag(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tags'] });
      setDeleteTarget(null);
      toast.success('Tag deleted.');
    },
    onError: () => toast.error('Failed to delete tag.'),
  });

  if (isLoading) return <LoadingOverlay />;

  const tags = data?.data ?? [];

  return (
    <Box>
      <PageHeader
        title="Tags"
        breadcrumbs={[{ label: 'Tags' }]}
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => { reset(); setDialogOpen(true); }}>
            New Tag
          </Button>
        }
      />

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={createMutation.error ?? deleteMutation.error} />

      <Paper sx={{ p: 2, mb: 2 }}>
        <TextField
          size="small"
          placeholder="Search tags..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          InputProps={{ startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> }}
          sx={{ minWidth: 280 }}
        />
      </Paper>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Slug</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {tags.map((tag) => (
              <TableRow key={tag.id} hover>
                <TableCell><Typography variant="body2" fontWeight={500}>{tag.name}</Typography></TableCell>
                <TableCell><Typography variant="body2" color="text.secondary">{tag.slug}</Typography></TableCell>
                <TableCell align="center">
                  <Chip label={tag.isActive ? 'Active' : 'Inactive'} color={tag.isActive ? 'success' : 'default'} size="small" />
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                    <Tooltip title="Delete">
                      <IconButton size="small" color="error" onClick={() => setDeleteTarget(tag)}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
            {!tags.length && (
              <TableRow>
                <TableCell colSpan={4} align="center">
                  <Typography variant="body2" color="text.secondary" py={3}>No tags found.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Create Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="xs" fullWidth>
        <form onSubmit={handleSubmit((d) => createMutation.mutate(d))} noValidate>
          <DialogTitle>New Tag</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={createMutation.error} />
            <TextField {...register('name')} label="Tag Name" fullWidth
              error={!!errors.name} helperText={errors.name?.message} sx={{ mt: 1 }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting || createMutation.isPending}>
              Create
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Tag"
        message={`Delete tag "${deleteTarget?.name}"?`}
        confirmLabel="Delete"
        onConfirm={() => deleteMutation.mutate(deleteTarget!.id)}
        onCancel={() => setDeleteTarget(null)}
        danger
      />
    </Box>
  );
}
