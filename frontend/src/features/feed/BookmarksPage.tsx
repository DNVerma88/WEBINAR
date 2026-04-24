import { Box, Stack, Typography } from '@/components/ui';
import { useQuery } from '@tanstack/react-query';
import { feedApi } from '../../shared/api/feedApi';
import { PostCard } from '../communities/components/PostCard';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';

export default function BookmarksPage() {
  usePageTitle('Bookmarks');

  const { data, isLoading, error } = useQuery({
    queryKey: ['feed', 'bookmarks'],
    queryFn: ({ signal }) => feedApi.getBookmarks(1, 50, signal),
  });

  if (isLoading) return <LoadingOverlay />;

  return (
    <Box>
      <PageHeader title="Bookmarks" />
      {error && <ApiErrorAlert error={error} />}
      <Stack spacing={2}>
        {data?.data.map((post) => (
          <PostCard key={post.id} post={post} />
        ))}
        {!isLoading && data?.data.length === 0 && (
          <Typography color="text.secondary" textAlign="center" py={4}>
            No bookmarks yet. Bookmark posts to find them here later.
          </Typography>
        )}
      </Stack>
    </Box>
  );
}
