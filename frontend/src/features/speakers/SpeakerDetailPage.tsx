import {
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Grid,
  Rating,
  Stack,
  Typography,
} from '@/components/ui';
import { useParams, Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { speakersApi } from '../../shared/api/speakers';
import { usersApi } from '../../shared/api/users';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';

export default function SpeakerDetailPage() {
  usePageTitle('Speaker Profile');
  const { id } = useParams<{ id: string }>();
  const { user: currentUser } = useAuth();
  const qc = useQueryClient();

  const { data: speaker, isLoading, error } = useQuery({
    queryKey: ['speaker', id],
    queryFn: ({ signal }) => speakersApi.getSpeakerById(id!, signal),
    enabled: !!id,
  });

  const toast = useToast();
  const followMutation = useMutation({
    mutationFn: () => usersApi.followUser(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['speaker', id] }); toast.success('Following speaker.'); },
    onError: () => toast.error('Failed to follow speaker.'),
  });

  const unfollowMutation = useMutation({
    mutationFn: () => usersApi.unfollowUser(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['speaker', id] }); toast.success('Unfollowed speaker.'); },
    onError: () => toast.error('Failed to unfollow speaker.'),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error || !speaker) return <ApiErrorAlert error={error ?? new Error('Speaker not found')} />;

  const isOwnProfile = currentUser?.userId === id;

  return (
    <Box>
      <PageHeader title="Speaker Profile" />

      <Card sx={{ mb: 3 }}>
        <CardContent sx={{ p: 3 }}>
          <Box display="flex" alignItems="flex-start" gap={3} flexWrap="wrap">
            <Avatar src={speaker.profilePhotoUrl ?? undefined} sx={{ width: 80, height: 80, fontSize: 32 }}>
              {speaker.fullName[0]}
            </Avatar>
            <Box flex={1}>
              <Box display="flex" alignItems="center" gap={2} flexWrap="wrap">
                <Typography variant="h5" fontWeight={700}>{speaker.fullName}</Typography>
                {speaker.isKnowledgeBroker && (
                  <Chip label="Knowledge Broker" color="primary" size="small" />
                )}
              </Box>
              <Typography variant="body1" color="text.secondary" mt={0.5}>
                {speaker.designation}
                {speaker.department ? ` · ${speaker.department}` : ''}
              </Typography>

              <Stack direction="row" spacing={1} mt={1} alignItems="center">
                <Rating value={speaker.averageRating} precision={0.5} size="small" readOnly />
                <Typography variant="body2" color="text.secondary">
                  {speaker.averageRating.toFixed(1)} · {speaker.totalSessionsDelivered} sessions delivered
                </Typography>
              </Stack>

              <Stack direction="row" spacing={1} mt={1}>
                <Chip label={`${speaker.followerCount} followers`} size="small" variant="outlined" />
              </Stack>

              {!isOwnProfile && (
                <Box mt={2}>
                  {speaker.isFollowedByCurrentUser ? (
                    <Button
                      variant="outlined"
                      size="small"
                      onClick={() => unfollowMutation.mutate()}
                      disabled={unfollowMutation.isPending}
                    >
                      Unfollow
                    </Button>
                  ) : (
                    <Button
                      variant="contained"
                      size="small"
                      onClick={() => followMutation.mutate()}
                      disabled={followMutation.isPending}
                    >
                      Follow
                    </Button>
                  )}
                </Box>
              )}
            </Box>
          </Box>

          {speaker.bio && (
            <>
              <Divider sx={{ my: 2 }} />
              <Typography variant="body1">{speaker.bio}</Typography>
            </>
          )}

          {(speaker.areasOfExpertise || speaker.technologiesKnown) && (
            <>
              <Divider sx={{ my: 2 }} />
              <Grid container spacing={2}>
                {speaker.areasOfExpertise && (
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Typography variant="subtitle2" gutterBottom>Areas of Expertise</Typography>
                    <Typography variant="body2" color="text.secondary">{speaker.areasOfExpertise}</Typography>
                  </Grid>
                )}
                {speaker.technologiesKnown && (
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Typography variant="subtitle2" gutterBottom>Technologies</Typography>
                    <Typography variant="body2" color="text.secondary">{speaker.technologiesKnown}</Typography>
                  </Grid>
                )}
              </Grid>
            </>
          )}
        </CardContent>
      </Card>

      {speaker.recentSessions.length > 0 && (
        <>
          <Typography variant="h6" mb={2}>Recent Sessions</Typography>
          <Grid container spacing={2}>
            {speaker.recentSessions.map((session) => (
              <Grid size={{ xs: 12, sm: 6, md: 4 }} key={session.id}>
                <Card
                  component={Link}
                  to={`/sessions/${session.id}`}
                  sx={{ textDecoration: 'none', height: '100%', display: 'block', '&:hover': { boxShadow: 4 }, transition: 'box-shadow 0.2s' }}
                >
                  <CardContent>
                    <Typography variant="subtitle1" fontWeight={600} gutterBottom>
                      {session.title}
                    </Typography>
                    {session.categoryName && (
                      <Chip label={session.categoryName} size="small" variant="outlined" sx={{ mb: 1 }} />
                    )}
                    <Typography variant="body2" color="text.secondary">
                      {new Date(session.scheduledAt).toLocaleDateString()} · {session.durationMinutes} min
                    </Typography>
                    {session.averageRating > 0 && (
                      <Stack direction="row" spacing={0.5} alignItems="center" mt={0.5}>
                        <Rating value={session.averageRating} precision={0.5} size="small" readOnly />
                        <Typography variant="caption">{session.averageRating.toFixed(1)}</Typography>
                      </Stack>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </>
      )}

      {speaker.recentSessions.length === 0 && (
        <Typography color="text.secondary" textAlign="center" mt={4}>
          No sessions delivered yet.
        </Typography>
      )}
    </Box>
  );
}
