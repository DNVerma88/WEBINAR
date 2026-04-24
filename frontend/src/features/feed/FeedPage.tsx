import { Box, Skeleton, Stack, Typography, Tabs, Tab } from '@/components/ui';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { feedApi } from '../../shared/api/feedApi';
import { PostCard } from '../communities/components/PostCard';
import { PageHeader } from '../../shared/components/PageHeader';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';

type FeedTab = 'personalized' | 'latest' | 'trending';

export default function FeedPage() {
  usePageTitle('Feed');
  const [tab, setTab] = useState<FeedTab>('personalized');

  // FE-01: each tab query is only enabled when its tab is active to avoid
  // 3 simultaneous API calls on every page mount
  const { data: personalizedFeed, isLoading: loadingPersonalized, error: errorPersonalized } = useInfiniteFeed('personalized', tab === 'personalized');
  const { data: latestFeed, isLoading: loadingLatest, error: errorLatest } = useInfiniteFeed('latest', tab === 'latest');
  const { data: trendingFeed, isLoading: loadingTrending, error: errorTrending } = useQuery({
    queryKey: ['feed', 'trending'],
    queryFn: ({ signal }) => feedApi.getTrending(1, 20, signal),
    enabled: tab === 'trending',
  });

  const isLoading = tab === 'personalized' ? loadingPersonalized : tab === 'latest' ? loadingLatest : loadingTrending;
  const error = tab === 'personalized' ? errorPersonalized : tab === 'latest' ? errorLatest : errorTrending;
  const posts =
    tab === 'personalized'
      ? personalizedFeed?.data ?? []
      : tab === 'latest'
      ? latestFeed?.data ?? []
      : trendingFeed?.data ?? [];

  if (isLoading) return (
    <Box>
      <PageHeader title="Feed" />
      <Stack spacing={2}>
        {Array.from({ length: 4 }).map((_, i) => (
          <Stack key={i} spacing={1}>
            <Skeleton variant="rectangular" height={100} sx={{ borderRadius: 1 }} />
            <Skeleton width="60%" />
            <Skeleton width="40%" />
          </Stack>
        ))}
      </Stack>
    </Box>
  );

  return (
    <Box>
      <PageHeader title="Feed" />

      <Tabs
        value={tab}
        onChange={(_, v) => setTab(v)}
        sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}
      >
        <Tab label="For You" value="personalized" />
        <Tab label="Latest" value="latest" />
        <Tab label="Trending" value="trending" />
      </Tabs>

      {error && <ApiErrorAlert error={error} />}

      <Stack spacing={2}>
        {posts.map((post) => (
          <PostCard key={post.id} post={post} />
        ))}
        {posts.length === 0 && !isLoading && (
          <Typography color="text.secondary" textAlign="center" py={4}>
            Nothing to show yet. Follow communities, tags, or authors to personalise your feed.
          </Typography>
        )}
      </Stack>
    </Box>
  );
}

// Fetches the first page of the given feed type; only runs when enabled
function useInfiniteFeed(type: 'personalized' | 'latest', enabled: boolean) {
  return useQuery({
    queryKey: ['feed', type],
    queryFn: ({ signal }) => (type === 'personalized' ? feedApi.getPersonalizedFeed({ pageSize: 20 }, signal) : feedApi.getLatest({ pageSize: 20 }, signal)),
    enabled,
  });
}
