import { Box, Button, Chip, Divider, Paper, Stack, Typography } from '@/components/ui';
import { BookmarkIcon } from '@/components/ui';
import { useParams, Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { communityPostsApi } from '../../shared/api/communityPosts';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { PostReactionBar } from './components/PostReactionBar';
import { CommentThread } from './components/CommentThread';
import { PostSeriesBanner } from './components/PostSeriesBanner';
import { useEffect } from 'react';

export default function PostDetailPage() {
  const { id: communityId, postId } = useParams<{ id: string; postId: string }>();
  const queryClient = useQueryClient();

  const { data: post, isLoading, error } = useQuery({
    queryKey: ['community-post', communityId, postId],
    queryFn: ({ signal }) => communityPostsApi.getPost(communityId!, postId!, signal),
    enabled: !!communityId && !!postId,
  });

  const { data: reactions } = useQuery({
    queryKey: ['post-reactions', postId],
    queryFn: ({ signal }) => communityPostsApi.getReactions(communityId!, postId!, signal),
    enabled: !!communityId && !!postId,
  });

  const { data: comments } = useQuery({
    queryKey: ['post-comments', postId],
    queryFn: ({ signal }) => communityPostsApi.getComments(communityId!, postId!, undefined, undefined, signal),
    enabled: !!communityId && !!postId,
  });

  const { mutate: toggleBookmark } = useMutation({
    mutationFn: () => communityPostsApi.toggleBookmark(communityId!, postId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['community-post', communityId, postId] });
    },
  });

  usePageTitle(post?.title ?? 'Post');

  // Phase 3 — inject <link rel="canonical"> when post has a canonical URL
  useEffect(() => {
    if (!post?.canonicalUrl) return;
    const existing = document.querySelector('link[rel="canonical"]');
    const link = existing ?? document.createElement('link');
    link.setAttribute('rel', 'canonical');
    link.setAttribute('href', post.canonicalUrl);
    if (!existing) document.head.appendChild(link);
    return () => { if (!existing) link.remove(); };
  }, [post?.canonicalUrl]);

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!post) return null;

  return (
    <Box>
      <PageHeader
        title=""
        breadcrumbs={[
          { label: 'Communities', to: '/communities' },
          { label: 'Community', to: `/communities/${communityId}` },
          { label: post.title },
        ]}
      />

      <Paper sx={{ p: { xs: 2, md: 4 }, mb: 3, maxWidth: 900, mx: 'auto' }}>
        <PostSeriesBanner post={post} />
        {post.coverImageUrl && (
          <Box
            component="img"
            src={post.coverImageUrl}
            alt={post.title}
            sx={{ width: '100%', maxHeight: 360, objectFit: 'cover', borderRadius: 1, mb: 3 }}
          />
        )}

        <Stack direction="row" spacing={1} mb={2} flexWrap="wrap">
          {post.tags.map((tag) => (
            <Chip
              key={tag.tagId}
              label={`#${tag.slug}`}
              size="small"
              component={Link}
              to={`/tags/${tag.slug}`}
              clickable
            />
          ))}
        </Stack>

        <Typography variant="h4" fontWeight={700} gutterBottom>
          {post.title}
        </Typography>

        <Stack direction="row" spacing={2} alignItems="center" mb={3} flexWrap="wrap">
          <Typography variant="body2" color="text.secondary">
            By <strong>{post.authorName}</strong>
          </Typography>
          {post.publishedAt && (
            <Typography variant="body2" color="text.secondary">
              {new Date(post.publishedAt).toLocaleDateString()}
            </Typography>
          )}
          <Typography variant="body2" color="text.secondary">
            🕐 {post.readingTimeMinutes} min read
          </Typography>
          <Typography variant="body2" color="text.secondary">
            👁️ {post.viewCount.toLocaleString()} views
          </Typography>

          <Box flex={1} />

          <Button
            size="small"
            variant={post.hasBookmarked ? 'contained' : 'outlined'}
            startIcon={<BookmarkIcon />}
            onClick={() => toggleBookmark()}
          >
            {post.hasBookmarked ? 'Bookmarked' : 'Bookmark'}
          </Button>

          <Button
            size="small"
            variant="outlined"
            component={Link}
            to={`/communities/${communityId}/posts/${postId}/edit`}
          >
            Edit
          </Button>
        </Stack>

        <Divider sx={{ mb: 3 }} />

        <Box
          className="prose"
          sx={{ lineHeight: 1.8, '& h1,h2,h3,h4': { mt: 3, mb: 1 }, '& pre': { overflow: 'auto', p: 2, bgcolor: 'grey.100', borderRadius: 1 }, '& code': { fontSize: 13 }, '& img': { maxWidth: '100%', borderRadius: 1 }, '& blockquote': { borderLeft: '4px solid', borderColor: 'grey.400', ml: 0, pl: 2, color: 'text.secondary' } }}
          dangerouslySetInnerHTML={{ __html: post.contentHtml }}
        />

        {post.seriesTitle && (
          <Box mt={3} p={2} bgcolor="action.hover" borderRadius={1}>
            <Typography variant="caption" color="text.secondary">Part of series: </Typography>
            <Typography variant="body2" fontWeight={600}>{post.seriesTitle}</Typography>
          </Box>
        )}

        <Divider sx={{ my: 4 }} />

        {reactions && (
          <Box mb={4}>
            <Typography variant="subtitle2" gutterBottom>Reactions</Typography>
            <PostReactionBar communityId={communityId!} postId={postId!} reactions={reactions} />
          </Box>
        )}

        <Divider sx={{ mb: 3 }} />

        <Box>
          <Typography variant="h6" gutterBottom>
            Comments ({post.commentCount})
          </Typography>
          <CommentThread
            communityId={communityId!}
            postId={postId!}
            comments={comments?.data ?? []}
          />
        </Box>
      </Paper>
    </Box>
  );
}
