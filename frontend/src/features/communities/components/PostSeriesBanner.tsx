import { Box, Chip, Stack, Typography } from '@/components/ui';
import { Link } from 'react-router-dom';
import type { CommunityPostDetailDto } from '../../../shared/types';

interface PostSeriesBannerProps {
  post: CommunityPostDetailDto;
}

export function PostSeriesBanner({ post }: PostSeriesBannerProps) {
  if (!post.seriesId || !post.seriesTitle) return null;

  return (
    <Box
      sx={{
        p: 2,
        mb: 3,
        borderLeft: 4,
        borderColor: 'secondary.main',
        bgcolor: 'secondary.50',
        borderRadius: 1,
      }}
    >
      <Stack direction="row" spacing={1} alignItems="center">
        <Chip label="Series" color="secondary" size="small" />
        <Typography variant="body2" color="text.secondary">
          This post is part of:&nbsp;
          <Link
            to={`/communities/${post.communityId}/series/${post.seriesId}`}
            style={{ fontWeight: 600, textDecoration: 'none' }}
          >
            {post.seriesTitle}
          </Link>
        </Typography>
      </Stack>
    </Box>
  );
}
