import { Box, Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@/components/ui';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import MDEditor from '@uiw/react-md-editor';
import { communityPostsApi } from '../../shared/api/communityPosts';
import { tagsApi } from '../../shared/api/tags';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { PostType, PostStatus } from '../../shared/types';
import { useEffect, useRef, useState } from 'react';

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(300),
  contentMarkdown: z.string().min(1, 'Content is required'),
  postType: z.nativeEnum(PostType),
  coverImageUrl: z.string().url().optional().or(z.literal('')),
  canonicalUrl: z.string().url().optional().or(z.literal('')),
  tagSlugs: z.array(z.string()).max(4, 'Maximum 4 tags'),
  publishImmediately: z.boolean(),
});
type FormValues = z.infer<typeof schema>;

export default function PostEditorPage() {
  const { id: communityId, postId } = useParams<{ id: string; postId?: string }>();
  const navigate = useNavigate();
  const isEdit = !!postId;

  const { data: existing, isLoading: loadingExisting } = useQuery({
    queryKey: ['community-post', communityId, postId],
    queryFn: ({ signal }) => communityPostsApi.getPost(communityId!, postId!, signal),
    enabled: isEdit,
  });

  const { data: tagsResult } = useQuery({
    queryKey: ['tags'],
    queryFn: ({ signal }) => tagsApi.getTags({ pageSize: 100 }, signal),
  });

  const tags = tagsResult?.data ?? [];

  const { control, handleSubmit, watch, setValue, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: existing?.title ?? '',
      contentMarkdown: existing?.contentMarkdown ?? '',
      postType: existing?.postType ?? PostType.Article,
      coverImageUrl: existing?.coverImageUrl ?? '',
      canonicalUrl: existing?.canonicalUrl ?? '',
      tagSlugs: existing?.tags.map((t) => t.slug) ?? [],
      publishImmediately: false,
    },
    values: existing
      ? {
          title: existing.title,
          contentMarkdown: existing.contentMarkdown,
          postType: existing.postType,
          coverImageUrl: existing.coverImageUrl ?? '',
          canonicalUrl: existing.canonicalUrl ?? '',
          tagSlugs: existing.tags.map((t) => t.slug),
          publishImmediately: false,
        }
      : undefined,
  });

  const selectedTags = watch('tagSlugs');
  const titleValue = watch('title');
  const contentValue = watch('contentMarkdown');

  // Phase 3 — Draft auto-save with 2s debounce (only for existing drafts)
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [savedSecondsAgo, setSavedSecondsAgo] = useState<number | null>(null);
  const autoSaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { mutate: saveDraft } = useMutation({
    mutationFn: (data: { title?: string; contentMarkdown?: string }) =>
      communityPostsApi.saveDraft(communityId!, postId!, data),
    onSuccess: () => setLastSaved(new Date()),
  });

  // Debounce auto-save
  useEffect(() => {
    if (!isEdit || !postId) return;
    if (autoSaveTimer.current) clearTimeout(autoSaveTimer.current);
    autoSaveTimer.current = setTimeout(() => {
      saveDraft({ title: titleValue, contentMarkdown: contentValue });
    }, 2000);
    return () => { if (autoSaveTimer.current) clearTimeout(autoSaveTimer.current); };
  }, [titleValue, contentValue, isEdit, postId]);

  // Update "X seconds ago" label every 10s
  useEffect(() => {
    if (!lastSaved) return;
    const update = () => setSavedSecondsAgo(Math.round((Date.now() - lastSaved.getTime()) / 1000));
    update();
    const timer = setInterval(update, 10000);
    return () => clearInterval(timer);
  }, [lastSaved]);

  const { mutate: save, isPending, error } = useMutation({
    mutationFn: (values: FormValues) => {
      if (isEdit) {
        return communityPostsApi.updatePost(communityId!, postId!, {
          title: values.title,
          contentMarkdown: values.contentMarkdown,
          coverImageUrl: values.coverImageUrl || undefined,
          canonicalUrl: values.canonicalUrl || undefined,
          tagSlugs: values.tagSlugs,
          status: values.publishImmediately ? PostStatus.Published : undefined,
        });
      }
      return communityPostsApi.createPost(communityId!, {
        title: values.title,
        contentMarkdown: values.contentMarkdown,
        postType: values.postType,
        coverImageUrl: values.coverImageUrl || undefined,
        canonicalUrl: values.canonicalUrl || undefined,
        tagSlugs: values.tagSlugs,
        publishImmediately: values.publishImmediately,
      });
    },
    onSuccess: (result) => {
      navigate(`/communities/${communityId}/posts/${result.id}`);
    },
  });

  usePageTitle(isEdit ? 'Edit Post' : 'Write a Post');

  if (isEdit && loadingExisting) return <LoadingOverlay />;

  return (
    <Box>
      <PageHeader
        title={isEdit ? 'Edit Post' : 'Write a Post'}
        breadcrumbs={[
          { label: 'Communities', to: '/communities' },
          { label: 'Community', to: `/communities/${communityId}` },
          { label: isEdit ? 'Edit Post' : 'New Post' },
        ]}
      />

      <Paper sx={{ p: { xs: 2, md: 4 }, maxWidth: 900, mx: 'auto' }}>
        {error && <Box sx={{ mb: 2 }}><ApiErrorAlert error={error} /></Box>}

        <Stack spacing={3} component="form" onSubmit={handleSubmit((v) => save(v))}>
          <Controller
            name="title"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Title"
                fullWidth
                error={!!errors.title}
                helperText={errors.title?.message}
              />
            )}
          />

          {!isEdit && (
            <Controller
              name="postType"
              control={control}
              render={({ field }) => (
                <TextField {...field} select label="Post Type" fullWidth>
                  <MenuItem value={PostType.Article}>Article</MenuItem>
                  <MenuItem value={PostType.Discussion}>Discussion</MenuItem>
                  <MenuItem value={PostType.Question}>Question</MenuItem>
                  <MenuItem value={PostType.TIL}>TIL (Today I Learned)</MenuItem>
                  <MenuItem value={PostType.Showcase}>Showcase</MenuItem>
                </TextField>
              )}
            />
          )}

          <Controller
            name="coverImageUrl"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Cover Image URL (optional)"
                fullWidth
                error={!!errors.coverImageUrl}
                helperText={errors.coverImageUrl?.message}
              />
            )}
          />

          <Box>
            <Typography variant="subtitle2" gutterBottom>Content</Typography>
            <Controller
              name="contentMarkdown"
              control={control}
              render={({ field }) => (
                <Box data-color-mode="light">
                  <MDEditor
                    value={field.value}
                    onChange={(v) => field.onChange(v ?? '')}
                    height={400}
                    preview="live"
                  />
                </Box>
              )}
            />
            {errors.contentMarkdown && (
              <Typography variant="caption" color="error">{errors.contentMarkdown.message}</Typography>
            )}
          </Box>

          <Box>
            <Typography variant="subtitle2" gutterBottom>
              Tags (max 4)
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap" gap={1}>
              {tags.map((tag) => {
                const selected = selectedTags.includes(tag.slug);
                return (
                  <Chip
                    key={tag.id}
                    label={`#${tag.slug}`}
                    size="small"
                    variant={selected ? 'filled' : 'outlined'}
                    color={selected ? 'primary' : 'default'}
                    onClick={() => {
                      if (selected) {
                        setValue('tagSlugs', selectedTags.filter((s) => s !== tag.slug));
                      } else if (selectedTags.length < 4) {
                        setValue('tagSlugs', [...selectedTags, tag.slug]);
                      }
                    }}
                    sx={{ cursor: 'pointer' }}
                  />
                );
              })}
            </Stack>
          </Box>

          <Controller
            name="canonicalUrl"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Canonical URL (optional — if cross-posting from another site)"
                fullWidth
                error={!!errors.canonicalUrl}
                helperText={errors.canonicalUrl?.message}
              />
            )}
          />

          <Stack direction="row" spacing={2} justifyContent="flex-end">
            <Controller
              name="publishImmediately"
              control={control}
              render={({ field }) => (
                <>
                  <Button
                    type="submit"
                    variant="outlined"
                    disabled={isPending}
                    onClick={() => field.onChange(false)}
                  >
                    Save Draft
                  </Button>
                  <Button
                    type="submit"
                    variant="contained"
                    disabled={isPending}
                    onClick={() => field.onChange(true)}
                  >
                    Publish
                  </Button>
                </>
              )}
            />
            {isEdit && savedSecondsAgo !== null && (
              <Typography variant="caption" color="text.secondary" alignSelf="center">
                Draft saved {savedSecondsAgo < 10 ? 'just now' : `${savedSecondsAgo}s ago`}
              </Typography>
            )}
          </Stack>
        </Stack>
      </Paper>
    </Box>
  );
}
