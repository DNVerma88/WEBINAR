import { Box, Stack, Typography, Divider, Chip } from '@/components/ui';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { postSeriesApi } from '../../shared/api/feedApi';
import { PostCard } from './components/PostCard';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';

export default function SeriesDetailPage() {
  const { id: communityId, seriesId } = useParams<{ id: string; seriesId: string }>();

  const { data: series, isLoading, error } = useQuery({
    queryKey: ['community-series', communityId, seriesId],
    queryFn: ({ signal }) => postSeriesApi.getSeriesById(communityId!, seriesId!, signal),
    enabled: !!communityId && !!seriesId,
  });

  usePageTitle(series?.title ?? 'Series');

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!series) return null;

  return (
    <Box>
      <PageHeader
        title=""
        breadcrumbs={[
          { label: 'Communities', to: '/communities' },
          { label: 'Community', to: `/communities/${communityId}` },
          { label: 'Series', to: `/communities/${communityId}/series` },
          { label: series.title },
        ]}
      />

      <Box mb={4}>
        <Stack direction="row" spacing={1} alignItems="center" mb={1}>
          <Chip label="Series" color="secondary" size="small" />
          <Chip label={`${series.postCount} posts`} variant="outlined" size="small" />
        </Stack>
        <Typography variant="h4" fontWeight={700}>{series.title}</Typography>
        {series.description && (
          <Typography color="text.secondary" mt={1}>{series.description}</Typography>
        )}
        <Typography variant="body2" color="text.secondary" mt={0.5}>
          by {series.authorName}
        </Typography>
      </Box>

      <Divider sx={{ mb: 3 }} />

      <Stack spacing={2}>
        {series.posts.map((post, index) => (
          <Box key={post.id} display="flex" gap={2} alignItems="flex-start">
            <Typography
              variant="h6"
              color="text.secondary"
              sx={{ minWidth: 32, mt: 1, fontWeight: 300 }}
            >
              {index + 1}
            </Typography>
            <Box flex={1}>
              <PostCard post={post} />
            </Box>
          </Box>
        ))}
        {series.posts.length === 0 && (
          <Typography color="text.secondary" textAlign="center" py={4}>
            This series has no posts yet.
          </Typography>
        )}
      </Stack>
    </Box>
  );
}
