import { Box, IconButton, Stack, Tooltip, Typography } from '@/components/ui';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { PostReactionResultDto } from '../../../shared/types';
import { ReactionType, REACTION_EMOJIS } from '../../../shared/types';
import { communityPostsApi } from '../../../shared/api/communityPosts';

interface PostReactionBarProps {
  communityId: string;
  postId: string;
  reactions: PostReactionResultDto;
}

export function PostReactionBar({ communityId, postId, reactions }: PostReactionBarProps) {
  const queryClient = useQueryClient();

  const { mutate: toggleReaction } = useMutation({
    mutationFn: (reactionType: ReactionType) =>
      communityPostsApi.toggleReaction(communityId, postId, { reactionType }),
    onSuccess: (data) => {
      queryClient.setQueryData(['post-reactions', postId], data);
    },
  });

  return (
    <Stack direction="row" spacing={1} flexWrap="wrap">
      {(Object.values(ReactionType).filter((v) => typeof v === 'number') as ReactionType[]).map((type) => {
        const count = reactions.reactionCounts[type] ?? 0;
        const isActive = reactions.userReactions.includes(type);
        return (
          <Tooltip key={type} title={ReactionType[type]}>
            <Box>
              <IconButton
                size="small"
                onClick={() => toggleReaction(type)}
                sx={{
                  border: isActive ? '1px solid' : '1px solid transparent',
                  borderColor: isActive ? 'primary.main' : 'transparent',
                  borderRadius: 2,
                  px: 1,
                  gap: 0.5,
                }}
              >
                <span style={{ fontSize: 18 }}>{REACTION_EMOJIS[type]}</span>
                {count > 0 && (
                  <Typography variant="caption" color={isActive ? 'primary' : 'text.secondary'}>
                    {count}
                  </Typography>
                )}
              </IconButton>
            </Box>
          </Tooltip>
        );
      })}
    </Stack>
  );
}
