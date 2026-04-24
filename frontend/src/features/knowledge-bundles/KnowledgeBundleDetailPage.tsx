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
  Divider,
  EditIcon,
  FormControl,
  Grid,
  InputLabel,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useToast } from '../../shared/hooks/useToast';
import { z } from 'zod';
import { knowledgeBundlesApi } from '../../shared/api/knowledgeBundles';
import { knowledgeAssetsApi } from '../../shared/api/knowledgeAssets';
import { categoriesApi } from '../../shared/api/categories';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import type {
  UpdateKnowledgeBundleRequest,
  AddBundleItemRequest,
} from '../../shared/types';

const editSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(1000).optional().or(z.literal('')),
  categoryId: z.string().optional(),
  coverImageUrl: z.string().url('Must be a valid URL').optional().or(z.literal('')),
  isPublished: z.boolean(),
});

const addItemSchema = z.object({
  knowledgeAssetId: z.string().min(1, 'Asset is required'),
  orderSequence: z.coerce.number().min(0),
  notes: z.string().max(500).optional().or(z.literal('')),
});

// F6/F9: reject non-HTTP(S) URLs to prevent XSS via javascript: URIs and spoofed image src
const safeHref = (url: string) => /^https?:\/\//i.test(url) ? url : '#';

export default function KnowledgeBundleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { hasRole } = useAuth();

  const toast = useToast();
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [showAddItemDialog, setShowAddItemDialog] = useState(false);
  const [deleteItemId, setDeleteItemId] = useState<string | null>(null);

  const { data: bundle, isLoading, error } = useQuery({
    queryKey: ['knowledge-bundles', id],
    queryFn: ({ signal }) => knowledgeBundlesApi.getBundleById(id!, signal),
    enabled: !!id,
  });

  const { data: assets } = useQuery({
    queryKey: ['knowledge-assets-all'],
    queryFn: ({ signal }) => knowledgeAssetsApi.getAssets({ pageSize: 200 }, signal),
    enabled: showAddItemDialog,
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => categoriesApi.getCategories(signal),
    enabled: showEditDialog,
  });

  usePageTitle(bundle?.title ?? 'Bundle');

  const isAdmin = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);
  const canEdit = isAdmin || hasRole(UserRole.Contributor) || hasRole(UserRole.KnowledgeTeam);

  const updateMutation = useMutation({
    mutationFn: (data: UpdateKnowledgeBundleRequest) => knowledgeBundlesApi.updateBundle(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-bundles', id] });
      setShowEditDialog(false);
      toast.success('Bundle updated successfully.');
    },
    onError: () => toast.error('Failed to update bundle.'),
  });

  const addItemMutation = useMutation({
    mutationFn: (data: AddBundleItemRequest) => knowledgeBundlesApi.addItem(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-bundles', id] });
      setShowAddItemDialog(false);
      resetAddItem();
      toast.success('Item added to bundle.');
    },
    onError: () => toast.error('Failed to add item.'),
  });

  const removeItemMutation = useMutation({
    mutationFn: (assetId: string) => knowledgeBundlesApi.removeItem(id!, assetId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['knowledge-bundles', id] });
      setDeleteItemId(null);
      toast.success('Item removed from bundle.');
    },
    onError: () => toast.error('Failed to remove item.'),
  });

  const {
    handleSubmit: handleEditSubmit,
    register: editRegister,
    control: editControl,
    setValue: setEditValue,
    formState: { errors: editErrors, isSubmitting: isEditSubmitting },
  } = useForm({ resolver: zodResolver(editSchema) });

  const {
    handleSubmit: handleAddItemSubmit,
    register: addItemRegister,
    control: addItemControl,
    reset: resetAddItem,
    formState: { errors: addItemErrors, isSubmitting: isAddItemSubmitting },
  } = useForm({ resolver: zodResolver(addItemSchema), defaultValues: { orderSequence: 0 } });

  if (isLoading) return <LoadingOverlay />;
  if (!bundle) return <Typography>Bundle not found.</Typography>;

  const openEditDialog = () => {
    setEditValue('title', bundle.title);
    setEditValue('description', bundle.description ?? '');
    setEditValue('categoryId', bundle.categoryId ?? '');
    setEditValue('coverImageUrl', bundle.coverImageUrl ?? '');
    setEditValue('isPublished', bundle.isPublished);
    setShowEditDialog(true);
  };

  const onEditSubmit = (data: z.infer<typeof editSchema>) => {
    updateMutation.mutate({
      title: data.title,
      description: data.description || undefined,
      categoryId: data.categoryId || undefined,
      coverImageUrl: data.coverImageUrl || undefined,
      isPublished: data.isPublished,
    });
  };

  const onAddItemSubmit = (data: z.infer<typeof addItemSchema>) => {
    addItemMutation.mutate({
      knowledgeAssetId: data.knowledgeAssetId,
      orderSequence: data.orderSequence,
      notes: data.notes || undefined,
    });
  };

  return (
    <Box>
      <PageHeader
        title={bundle.title}
        breadcrumbs={[{ label: 'Knowledge Bundles', to: '/knowledge-bundles' }, { label: bundle.title }]}
        actions={
          canEdit ? (
            <Stack direction="row" spacing={1}>
              <Button variant="outlined" startIcon={<AddIcon />} onClick={() => setShowAddItemDialog(true)}>
                Add Asset
              </Button>
              <Button variant="outlined" startIcon={<EditIcon />} onClick={openEditDialog}>
                Edit Bundle
              </Button>
            </Stack>
          ) : undefined
        }
      />

      <ApiErrorAlert error={error} />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          {/* Items List */}
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Assets ({bundle.items?.length ?? 0})
            </Typography>
            {bundle.items && bundle.items.length > 0 ? (
              <List>
                {[...bundle.items]
                  .sort((a, b) => a.orderSequence - b.orderSequence)
                  .map((item) => (
                    <ListItem
                      key={item.id}
                      divider
                      secondaryAction={
                        canEdit && (
                          <Button
                            size="small"
                            color="error"
                            onClick={() => setDeleteItemId(item.id)}
                          >
                            <DeleteIcon fontSize="small" />
                          </Button>
                        )
                      }
                    >
                      <ListItemText
                        primary={
                          <a href={safeHref(item.assetUrl)} target="_blank" rel="noreferrer">
                            {item.assetTitle}
                          </a>
                        }
                        secondary={item.notes ?? undefined}
                      />
                    </ListItem>
                  ))}
              </List>
            ) : (
              <Typography variant="body2" color="text.secondary">
                No assets in this bundle yet.
                {canEdit && ' Click "Add Asset" to get started.'}
              </Typography>
            )}
          </Paper>
        </Grid>

        {/* Sidebar */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" gap={1} mb={2} flexWrap="wrap">
              <Chip
                label={bundle.isPublished ? 'Published' : 'Draft'}
                color={bundle.isPublished ? 'success' : 'default'}
                size="small"
              />
              {bundle.categoryName && (
                <Chip label={bundle.categoryName} size="small" variant="outlined" />
              )}
            </Box>

            {bundle.description && (
              <>
                <Typography variant="body2" color="text.secondary" mb={2}>
                  {bundle.description}
                </Typography>
                <Divider sx={{ mb: 2 }} />
              </>
            )}

            <Typography variant="body2">
              <strong>Created by:</strong> {bundle.createdByName}
            </Typography>
            <Typography variant="body2">
              <strong>Created:</strong> {new Date(bundle.createdDate).toLocaleDateString()}
            </Typography>
            <Typography variant="body2">
              <strong>Items:</strong> {bundle.itemCount}
            </Typography>

            {/* F9: only render img for validated HTTPS URLs */}
            {bundle.coverImageUrl && /^https?:\/\//i.test(bundle.coverImageUrl) && (
              <Box mt={2}>
                <img
                  src={bundle.coverImageUrl}
                  alt={bundle.title}
                  style={{ maxWidth: '100%', borderRadius: 8 }}
                />
              </Box>
            )}
          </Paper>
        </Grid>
      </Grid>

      {/* Edit Bundle Dialog */}
      <Dialog open={showEditDialog} onClose={() => setShowEditDialog(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleEditSubmit(onEditSubmit)} noValidate>
          <DialogTitle>Edit Bundle</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={updateMutation.error} />
            <TextField
              {...editRegister('title')}
              label="Title"
              fullWidth
              error={!!editErrors.title}
              helperText={editErrors.title?.message as string}
              sx={{ mt: 1, mb: 2 }}
            />
            <TextField
              {...editRegister('description')}
              label="Description"
              multiline
              rows={3}
              fullWidth
              sx={{ mb: 2 }}
            />
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Category</InputLabel>
              <Controller name="categoryId" control={editControl}
                render={({ field }) => (
                  <Select {...field} label="Category">
                    <MenuItem value="">None</MenuItem>
                    {(categories ?? []).map((c) => (
                      <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
                    ))}
                  </Select>
                )} />
            </FormControl>
            <TextField
              {...editRegister('coverImageUrl')}
              label="Cover Image URL"
              fullWidth
              error={!!editErrors.coverImageUrl}
              helperText={editErrors.coverImageUrl?.message as string}
              sx={{ mb: 2 }}
            />
            <Stack direction="row" spacing={1} alignItems="center">
              <input type="checkbox" id="bundle-published" {...editRegister('isPublished')} />
              <label htmlFor="bundle-published">Published</label>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowEditDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isEditSubmitting || updateMutation.isPending}>
              Save
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Add Item Dialog */}
      <Dialog open={showAddItemDialog} onClose={() => setShowAddItemDialog(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleAddItemSubmit(onAddItemSubmit)} noValidate>
          <DialogTitle>Add Asset to Bundle</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={addItemMutation.error} />
            <FormControl fullWidth sx={{ mt: 1, mb: 2 }}>
              <InputLabel>Knowledge Asset</InputLabel>
              <Controller name="knowledgeAssetId" control={addItemControl}
                render={({ field }) => (
                  <Select {...field} label="Knowledge Asset">
                    {(assets?.data ?? []).map((a) => (
                      <MenuItem key={a.id} value={a.id}>{a.title}</MenuItem>
                    ))}
                  </Select>
                )} />
            </FormControl>
            <TextField
              {...addItemRegister('orderSequence')}
              label="Order"
              type="number"
              fullWidth
              error={!!addItemErrors.orderSequence}
              helperText={addItemErrors.orderSequence?.message as string}
              sx={{ mb: 2 }}
            />
            <TextField
              {...addItemRegister('notes')}
              label="Notes (optional)"
              multiline
              rows={2}
              fullWidth
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowAddItemDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isAddItemSubmitting || addItemMutation.isPending}>
              Add
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Remove Item Confirm */}
      <ConfirmDialog
        open={!!deleteItemId}
        title="Remove Asset"
        message="Remove this asset from the bundle?"
        confirmLabel="Remove"
        onConfirm={() => deleteItemId && removeItemMutation.mutate(deleteItemId)}
        onCancel={() => setDeleteItemId(null)}
        loading={removeItemMutation.isPending}
        danger
      />
    </Box>
  );
}
