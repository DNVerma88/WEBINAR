import { Avatar, Box, Button, Divider, Stack, TextField, Typography } from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { PostCommentDto } from '../../../shared/types';
import { communityPostsApi } from '../../../shared/api/communityPosts';

interface CommentItemProps {
  comment: PostCommentDto;
  communityId: string;
  postId: string;
  depth?: number;
}

function CommentItem({ comment, communityId, postId, depth = 0 }: CommentItemProps) {
  const [replyOpen, setReplyOpen] = useState(false);
  const [replyText, setReplyText] = useState('');
  const queryClient = useQueryClient();

  const { mutate: addReply, isPending } = useMutation({
    mutationFn: () =>
      communityPostsApi.addComment(communityId, postId, {
        bodyMarkdown: replyText.trim(),
        parentCommentId: comment.id,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['post-comments', postId] });
      setReplyText('');
      setReplyOpen(false);
    },
  });

  return (
    <Box ml={depth > 0 ? 4 : 0}>
      <Stack direction="row" spacing={1.5} alignItems="flex-start" py={1.5}>
        <Avatar src={comment.authorAvatarUrl} sx={{ width: 32, height: 32, fontSize: 13 }}>
          {comment.authorName.charAt(0)}
        </Avatar>
        <Box flex={1}>
          <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
            <Typography variant="subtitle2">{comment.authorName}</Typography>
            <Typography variant="caption" color="text.secondary">
              {new Date(comment.createdDate).toLocaleDateString()}
            </Typography>
          </Stack>
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', color: comment.isDeleted ? 'text.disabled' : 'inherit' }}>
            {comment.bodyMarkdown}
          </Typography>
          {!comment.isDeleted && depth < 1 && (
            <Button size="small" variant="text" onClick={() => setReplyOpen(!replyOpen)} sx={{ mt: 0.5, p: 0, minWidth: 0 }}>
              Reply
            </Button>
          )}
          {replyOpen && (
            <Stack direction="row" spacing={1} mt={1}>
              <TextField
                size="small"
                multiline
                minRows={2}
                fullWidth
                placeholder="Write a reply..."
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
              />
              <Stack spacing={0.5}>
                <Button
                  size="small"
                  variant="contained"
                  disabled={!replyText.trim() || isPending}
                  onClick={() => addReply()}
                >
                  Post
                </Button>
                <Button size="small" variant="text" onClick={() => setReplyOpen(false)}>
                  Cancel
                </Button>
              </Stack>
            </Stack>
          )}
        </Box>
      </Stack>
      {comment.replies.map((reply) => (
        <CommentItem key={reply.id} comment={reply} communityId={communityId} postId={postId} depth={depth + 1} />
      ))}
    </Box>
  );
}

interface CommentThreadProps {
  communityId: string;
  postId: string;
  comments: PostCommentDto[];
}

export function CommentThread({ communityId, postId, comments }: CommentThreadProps) {
  const [newComment, setNewComment] = useState('');
  const queryClient = useQueryClient();

  const { mutate: addComment, isPending } = useMutation({
    mutationFn: () =>
      communityPostsApi.addComment(communityId, postId, { bodyMarkdown: newComment.trim() }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['post-comments', postId] });
      setNewComment('');
    },
  });

  return (
    <Box>
      <Stack direction="row" spacing={1} mb={2}>
        <TextField
          size="small"
          multiline
          minRows={3}
          fullWidth
          placeholder="Write a comment..."
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
        />
        <Button
          variant="contained"
          disabled={!newComment.trim() || isPending}
          onClick={() => addComment()}
          sx={{ alignSelf: 'flex-end' }}
        >
          Post
        </Button>
      </Stack>

      {comments.map((c, i) => (
        <Box key={c.id}>
          {i > 0 && <Divider />}
          <CommentItem comment={c} communityId={communityId} postId={postId} />
        </Box>
      ))}
    </Box>
  );
}
