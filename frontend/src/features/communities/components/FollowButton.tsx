import { Button } from '@/components/ui';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { userFollowApi } from '../../../shared/api/feedApi';

interface FollowButtonProps {
  userId: string;
  isFollowing: boolean;
  onToggle?: (isFollowing: boolean) => void;
}

export function FollowButton({ userId, isFollowing, onToggle }: FollowButtonProps) {
  const queryClient = useQueryClient();

  const { mutate, isPending } = useMutation({
    mutationFn: () => userFollowApi.toggleFollow(userId),
    onSuccess: (data) => {
      onToggle?.(data.isFollowing);
      queryClient.invalidateQueries({ queryKey: ['feed'] });
      queryClient.invalidateQueries({ queryKey: ['user-followers', userId] });
      queryClient.invalidateQueries({ queryKey: ['user-following', userId] });
    },
  });

  return (
    <Button
      variant={isFollowing ? 'outlined' : 'contained'}
      size="small"
      onClick={() => mutate()}
      disabled={isPending}
    >
      {isFollowing ? 'Following' : 'Follow'}
    </Button>
  );
}
