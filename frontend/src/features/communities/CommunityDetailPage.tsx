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
  Paper,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@/components/ui';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { communitiesApi } from '../../shared/api/communities';
import { communityPostsApi } from '../../shared/api/communityPosts';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import type { CreateWikiPageRequest, UpdateWikiPageRequest } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';
import { Grid } from '@/components/ui';
import { PostCard } from './components/PostCard';

function CommunityPostsList({ communityId }: { communityId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['community-posts', communityId],
    queryFn: ({ signal }) => communityPostsApi.getPosts(communityId, { pageSize: 20 }, signal),
  });
  if (isLoading) return <LoadingOverlay />;
  if (!data?.data.length) return <Typography color="text.secondary">No posts yet. Be the first to write one!</Typography>;
  return (
    <Grid container spacing={2}>
      {data.data.map((post) => (
        <Grid size={{ xs: 12, sm: 6, md: 4 }} key={post.id}>
          <PostCard post={post} />
        </Grid>
      ))}
    </Grid>
  );
}

const wikiPageSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  contentMarkdown: z.string().min(1, 'Content is required').max(50000),
  isPublished: z.boolean(),
});
type WikiFormValues = {
  title: string;
  contentMarkdown: string;
  isPublished: boolean;
};

function TabPanel({ children, value, index }: { children: React.ReactNode; value: number; index: number }) {
  return value === index ? <Box mt={2}>{children}</Box> : null;
}

export default function CommunityDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { hasRole } = useAuth();
  const navigate = useNavigate();

  const [tab, setTab] = useState(0);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [showWikiDialog, setShowWikiDialog] = useState(false);
  const [editingPageId, setEditingPageId] = useState<string | null>(null);
  const [deleteWikiPageId, setDeleteWikiPageId] = useState<string | null>(null);
  const toast = useToast();

  const { data: community, isLoading, error } = useQuery({
    queryKey: ['communities', id],
    queryFn: ({ signal }) => communitiesApi.getCommunityById(id!, signal),
    enabled: !!id,
  });

  const { data: wikiPages } = useQuery({
    queryKey: ['communities', id, 'wiki'],
    queryFn: ({ signal }) => communitiesApi.getWikiPages(id!, signal),
    enabled: !!id && tab === 1,
  });

  usePageTitle(community?.name ?? 'Community');

  const joinMutation = useMutation({
    mutationFn: () => communitiesApi.joinCommunity(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['communities', id] }); toast.success('Joined community!'); },
    onError: () => toast.error('Failed to join community.'),
  });

  const leaveMutation = useMutation({
    mutationFn: () => communitiesApi.leaveCommunity(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['communities', id] }); toast.success('Left community.'); },
    onError: () => toast.error('Failed to leave community.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => communitiesApi.deleteCommunity(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['communities'] }); navigate('/communities'); },
    onError: () => toast.error('Failed to delete community.'),
  });

  const createWikiMutation = useMutation({
    mutationFn: (data: CreateWikiPageRequest) => communitiesApi.createWikiPage(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['communities', id, 'wiki'] });
      setShowWikiDialog(false);
      resetWiki();
      toast.success('Wiki page created.');
    },
    onError: () => toast.error('Failed to create wiki page.'),
  });

  const updateWikiMutation = useMutation({
    mutationFn: ({ pageId, data }: { pageId: string; data: UpdateWikiPageRequest }) =>
      communitiesApi.updateWikiPage(id!, pageId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['communities', id, 'wiki'] });
      setShowWikiDialog(false);
      setEditingPageId(null);
      resetWiki();
      toast.success('Wiki page updated.');
    },
    onError: () => toast.error('Failed to update wiki page.'),
  });

  const deleteWikiMutation = useMutation({
    mutationFn: (pageId: string) => communitiesApi.deleteWikiPage(id!, pageId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['communities', id, 'wiki'] });
      setDeleteWikiPageId(null);
      toast.success('Wiki page deleted.');
    },
    onError: () => toast.error('Failed to delete wiki page.'),
  });

  const {
    handleSubmit,
    register,
    reset: resetWiki,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<WikiFormValues>({ resolver: zodResolver(wikiPageSchema), defaultValues: { isPublished: true } });

  const isAdmin = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);
  const canEdit = isAdmin;
  const canManageWiki = isAdmin || hasRole(UserRole.Manager) || community?.isMember;

  if (isLoading) return <LoadingOverlay />;
  if (!community) return <Typography>Community not found.</Typography>;

  const onWikiSubmit = (data: WikiFormValues) => {
    if (editingPageId) {
      updateWikiMutation.mutate({ pageId: editingPageId, data });
    } else {
      createWikiMutation.mutate(data);
    }
  };

  const openEditWiki = (page: { id: string; title: string; contentMarkdown: string; isPublished: boolean }) => {
    setEditingPageId(page.id);
    setValue('title', page.title);
    setValue('contentMarkdown', page.contentMarkdown);
    setValue('isPublished', page.isPublished);
    setShowWikiDialog(true);
  };

  const openCreateWiki = () => {
    setEditingPageId(null);
    resetWiki();
    setShowWikiDialog(true);
  };

  return (
    <Box>
      <PageHeader
        title={community.name}
        breadcrumbs={[{ label: 'Communities', to: '/communities' }, { label: community.name }]}
        actions={
          canEdit ? (
            <Stack direction="row" spacing={1}>
              <Button variant="outlined" startIcon={<EditIcon />} component={Link} to={`/communities/${id}/edit`}>
                Edit
              </Button>
              <Button variant="outlined" color="error" onClick={() => setShowDeleteDialog(true)}>
                Delete
              </Button>
            </Stack>
          ) : undefined
        }
      />

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={joinMutation.error} />
      <ApiErrorAlert error={leaveMutation.error} />

      <Paper sx={{ p: 0 }}>
        <Tabs value={tab} onChange={(_, v) => setTab(v)}>
          <Tab label="Overview" />
          <Tab label="Posts" />
          <Tab label="Wiki" />
        </Tabs>

        <Box px={3} pb={3}>
          {/* Tab 0: Overview */}
          <TabPanel value={tab} index={0}>
            <Chip label={`${community.memberCount} members`} sx={{ mb: 2 }} />
            {community.description && (
              <Typography variant="body1" color="text.secondary" mb={3}>
                {community.description}
              </Typography>
            )}
            {community.isMember ? (
              <Button variant="outlined" color="inherit" onClick={() => leaveMutation.mutate()} disabled={leaveMutation.isPending}>
                Leave Community
              </Button>
            ) : (
              <Button variant="contained" onClick={() => joinMutation.mutate()} disabled={joinMutation.isPending}>
                Join Community
              </Button>
            )}
          </TabPanel>

          {/* Tab 1: Posts */}
          <TabPanel value={tab} index={1}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="h6">Posts</Typography>
              {community.isMember && (
                <Button variant="contained" size="small" startIcon={<AddIcon />} component={Link} to={`/communities/${id}/posts/new`}>
                  Write Post
                </Button>
              )}
            </Stack>
            <CommunityPostsList communityId={id!} />
          </TabPanel>

          {/* Tab 2: Wiki */}
          <TabPanel value={tab} index={2}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="h6">Community Wiki</Typography>
              {canManageWiki && (
                <Button variant="contained" size="small" startIcon={<AddIcon />} onClick={openCreateWiki}>
                  New Page
                </Button>
              )}
            </Stack>

            {wikiPages && wikiPages.length > 0 ? (
              wikiPages.map((page) => (
                <Paper key={page.id} variant="outlined" sx={{ p: 2, mb: 2 }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                    <Box flex={1}>
                      <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                        <Typography variant="subtitle1" fontWeight={600}>{page.title}</Typography>
                        <Chip label={page.isPublished ? 'Published' : 'Draft'} size="small" color={page.isPublished ? 'success' : 'default'} />
                      </Stack>
                      <Typography variant="body2" color="text.secondary" sx={{
                        display: '-webkit-box',
                        WebkitLineClamp: 3,
                        WebkitBoxOrient: 'vertical',
                        overflow: 'hidden',
                        whiteSpace: 'pre-wrap',
                      }}>
                        {page.contentMarkdown}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" mt={1} display="block">
                        By {page.authorName} Â· {page.viewCount} views
                      </Typography>
                    </Box>
                    {canManageWiki && (
                      <Stack direction="row" spacing={0.5} ml={1}>
                        <Button size="small" startIcon={<EditIcon />} onClick={() => openEditWiki(page)}>
                          Edit
                        </Button>
                        {isAdmin && (
                          <Button size="small" color="error" startIcon={<DeleteIcon />} onClick={() => setDeleteWikiPageId(page.id)}>
                            Delete
                          </Button>
                        )}
                      </Stack>
                    )}
                  </Stack>
                </Paper>
              ))
            ) : (
              <Typography variant="body2" color="text.secondary">No wiki pages yet. Create the first one!</Typography>
            )}
          </TabPanel>
        </Box>
      </Paper>

      {/* Wiki Page Dialog */}
      <Dialog open={showWikiDialog} onClose={() => setShowWikiDialog(false)} maxWidth="md" fullWidth>
        <form onSubmit={handleSubmit(onWikiSubmit)} noValidate>
          <DialogTitle>{editingPageId ? 'Edit Wiki Page' : 'Create Wiki Page'}</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={createWikiMutation.error ?? updateWikiMutation.error} />
            <TextField
              {...register('title')}
              label="Title"
              fullWidth
              error={!!errors.title}
              helperText={errors.title?.message}
              sx={{ mt: 1, mb: 2 }}
            />
            <TextField
              {...register('contentMarkdown')}
              label="Content (Markdown)"
              multiline
              rows={10}
              fullWidth
              error={!!errors.contentMarkdown}
              helperText={errors.contentMarkdown?.message}
              sx={{ mb: 2 }}
            />
            <Stack direction="row" spacing={1} alignItems="center">
              <input type="checkbox" id="wiki-published" {...register('isPublished')} />
              <label htmlFor="wiki-published">Publish immediately</label>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowWikiDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting || createWikiMutation.isPending || updateWikiMutation.isPending}>
              {editingPageId ? 'Update' : 'Create'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Delete Wiki Page Confirm */}
      <ConfirmDialog
        open={!!deleteWikiPageId}
        title="Delete Wiki Page"
        message="Are you sure you want to delete this wiki page? This cannot be undone."
        confirmLabel="Delete"
        onConfirm={() => deleteWikiPageId && deleteWikiMutation.mutate(deleteWikiPageId)}
        onCancel={() => setDeleteWikiPageId(null)}
        loading={deleteWikiMutation.isPending}
        danger
      />

      {/* Delete Community Confirm */}
      <ConfirmDialog
        open={showDeleteDialog}
        title="Delete Community"
        message={`Are you sure you want to delete "${community.name}"? This will deactivate the community for all members.`}
        confirmLabel="Delete"
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setShowDeleteDialog(false)}
        loading={deleteMutation.isPending}
        danger
      />
    </Box>
  );
}
