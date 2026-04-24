import { Avatar, Box, Card, CardActionArea, CardContent, Chip, Stack, Typography } from '@/components/ui';
import { BookmarkIcon } from '@/components/ui';
import { memo } from 'react';
import { Link } from 'react-router-dom';
import type { CommunityPostSummaryDto } from '../../../shared/types';
import { PostType, PostStatus, POST_TYPE_LABELS } from '../../../shared/types';

const postTypeColor: Record<PostType, 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'> = {
  [PostType.Article]: 'primary',
  [PostType.Discussion]: 'secondary',
  [PostType.Question]: 'warning',
  [PostType.TIL]: 'success',
  [PostType.Showcase]: 'info',
};

interface PostCardProps {
  post: CommunityPostSummaryDto;
}

// FE-09: React.memo prevents all feed/bookmark cards from re-rendering when any
// parent state changes (e.g. tab switch, pagination, other cards' mutation state)
export const PostCard = memo(function PostCard({ post }: PostCardProps) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea
        component={Link}
        to={`/communities/${post.communityId}/posts/${post.id}`}
        sx={{ height: '100%', display: 'flex', flexDirection: 'column', alignItems: 'stretch' }}
      >
        {post.coverImageUrl && (
          <Box
            component="img"
            src={post.coverImageUrl}
            alt={post.title}
            sx={{ width: '100%', height: 160, objectFit: 'cover' }}
          />
        )}
        <CardContent sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1} mb={1} flexWrap="wrap" alignItems="center">
            <Chip
              label={POST_TYPE_LABELS[post.postType as PostType]}
              color={postTypeColor[post.postType as PostType]}
              size="small"
            />
            {post.isFeatured && <Chip label="Featured" color="success" size="small" />}
            {post.status === PostStatus.Pinned && <Chip label="Pinned" color="warning" size="small" />}
          </Stack>

          <Typography variant="h6" gutterBottom sx={{ lineHeight: 1.3, fontWeight: 600 }}>
            {post.title}
          </Typography>

          <Stack direction="row" spacing={1} alignItems="center" mb={1}>
            <Avatar
              src={post.authorAvatarUrl}
              sx={{ width: 24, height: 24, fontSize: 12 }}
            >
              {post.authorName.charAt(0)}
            </Avatar>
            <Typography variant="caption" color="text.secondary">
              {post.authorName}
              {post.publishedAt && ` · ${new Date(post.publishedAt).toLocaleDateString()}`}
            </Typography>
          </Stack>

          {post.tags.length > 0 && (
            <Stack direction="row" spacing={0.5} flexWrap="wrap" mb={1}>
              {post.tags.slice(0, 3).map((tag) => (
                <Chip key={tag.tagId} label={`#${tag.slug}`} size="small" variant="outlined" sx={{ height: 20, fontSize: 11 }} />
              ))}
            </Stack>
          )}

          <Stack direction="row" spacing={2} mt={1}>
            <Typography variant="caption" color="text.secondary">
              ❤️ {post.reactionCount}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              💬 {post.commentCount}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              👁️ {post.viewCount}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              🕐 {post.readingTimeMinutes} min
            </Typography>
            {post.hasBookmarked && (
              <BookmarkIcon sx={{ fontSize: 14, color: 'primary.main' }} />
            )}
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
