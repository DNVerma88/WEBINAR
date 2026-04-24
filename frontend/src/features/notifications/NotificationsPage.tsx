import { Box, Button, Chip, Divider, IconButton, ReadAllIcon, ReadIcon, Skeleton, Stack, Tooltip, Typography } from '@/components/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../../shared/api/notifications';
import { PageHeader } from '../../shared/components/PageHeader';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { NotificationType } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useToast } from '../../shared/hooks/useToast';

const notificationLabel: Record<NotificationType, string> = {
  [NotificationType.ProposalSubmitted]: 'Proposal submitted',
  [NotificationType.ProposalApproved]: 'Proposal approved',
  [NotificationType.ProposalRejected]: 'Proposal rejected',
  [NotificationType.ProposalRevisionRequested]: 'Revision requested',
  [NotificationType.SessionScheduled]: 'Session scheduled',
  [NotificationType.SessionReminder]: 'Session reminder',
  [NotificationType.SessionStarted]: 'Session started',
  [NotificationType.SessionCancelled]: 'Session cancelled',
  [NotificationType.RegistrationConfirmed]: 'Registration confirmed',
  [NotificationType.WaitlistPromoted]: 'Moved off waitlist',
  [NotificationType.CommentAdded]: 'New comment',
  [NotificationType.BadgeAwarded]: 'Badge awarded',
  [NotificationType.KnowledgeRequestUpvoted]: 'Request upvoted',
  [NotificationType.KnowledgeRequestClaimed]: 'Request claimed',
  [NotificationType.NewFollower]: 'New follower',
  [NotificationType.MaterialAdded]: 'Material added',
  [NotificationType.General]: 'Notification',
  [NotificationType.MentoringRequestReceived]: 'Mentoring request',
  [NotificationType.MentoringRequestAccepted]: 'Mentoring accepted',
  [NotificationType.StreakMilestone]: 'Streak milestone',
  [NotificationType.LearningPathCompleted]: 'Path completed',
};

const getNotificationLabel = (type: NotificationType | string): string =>
  notificationLabel[type as NotificationType] ?? type;

export default function NotificationsPage() {
  usePageTitle('Notifications');
  const qc = useQueryClient();
  const toast = useToast();

  const { data, isLoading, error } = useQuery({
    queryKey: ['notifications'],
    queryFn: ({ signal }) => notificationsApi.getNotifications({ pageSize: 50 }, signal),
  });

  const markReadMutation = useMutation({
    mutationFn: notificationsApi.markAsRead,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const markAllReadMutation = useMutation({
    mutationFn: notificationsApi.markAllAsRead,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['notifications'] }); toast.success('All notifications marked as read.'); },
    onError: () => toast.error('Failed to mark all as read.'),
  });

  const unreadCount = data?.data.filter((n) => !n.isRead).length ?? 0;

  return (
    <Box>
      <PageHeader
        title="Notifications"
        actions={
          unreadCount > 0 ? (
            <Button
              startIcon={<ReadAllIcon />}
              onClick={() => markAllReadMutation.mutate()}
              disabled={markAllReadMutation.isPending}
            >
              Mark all as read
            </Button>
          ) : undefined
        }
      />

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={markReadMutation.error} />
      <ApiErrorAlert error={markAllReadMutation.error} />

      {isLoading ? (
        <Stack spacing={0} divider={<Divider />}>
          {Array.from({ length: 5 }).map((_, i) => (
            <Box key={i} px={2} py={1.5}>
              <Skeleton width="60%" />
              <Skeleton width="40%" />
            </Box>
          ))}
        </Stack>
      ) : data && data.data.length === 0 ? (
        <Typography color="text.secondary" textAlign="center" mt={6}>
          No notifications yet.
        </Typography>
      ) : (
        <Stack spacing={0} divider={<Divider />}>
          {data?.data.map((notification) => (
            <Box
              key={notification.id}
              display="flex"
              alignItems="center"
              px={2}
              py={1.5}
              bgcolor={notification.isRead ? 'transparent' : 'primary.50'}
              sx={{ bgcolor: notification.isRead ? 'transparent' : 'rgba(25,118,210,0.04)' }}
            >
              <Box flex={1}>
                <Box display="flex" alignItems="center" gap={1} mb={0.25}>
                  <Chip
                    label={getNotificationLabel(notification.notificationType)}
                    size="small"
                    variant="outlined"
                    color={notification.isRead ? 'default' : 'primary'}
                  />
                </Box>
                <Typography variant="body2" fontWeight={notification.isRead ? 400 : 500}>{notification.title}</Typography>
                <Typography variant="body2" color="text.secondary">{notification.body}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {new Date(notification.createdDate).toLocaleString()}
                </Typography>
              </Box>
              {!notification.isRead && (
                <Tooltip title="Mark as read">
                  <IconButton
                    size="small"
                    onClick={() => markReadMutation.mutate(notification.id)}
                    disabled={markReadMutation.isPending}
                  >
                    <ReadIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Box>
          ))}
        </Stack>
      )}
    </Box>
  );
}
