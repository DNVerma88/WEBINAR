import {
  Avatar,
  Box,
  Button,
  Chip,
  Divider,
  FormControlLabel,
  Grid,
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@/components/ui';
import { useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { usersApi } from '../../shared/api/users';
import { xpApi } from '../../shared/api/xp';
import type { UpdateContributorProfileRequest } from '../../shared/types';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { UnsavedChangesDialog } from '../../shared/components/UnsavedChangesDialog';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';

const profileSchema = z.object({
  fullName: z.string().min(1, 'Name is required').max(200),
  department: z.string().max(100).optional().or(z.literal('')),
  designation: z.string().max(100).optional().or(z.literal('')),
  yearsOfExperience: z.coerce.number().min(0).max(60).optional().or(z.literal('')),
  location: z.string().max(100).optional().or(z.literal('')),
  profilePhotoUrl: z.string().url('Must be a valid URL').optional().or(z.literal('')),
});

type ProfileFormValues = z.infer<typeof profileSchema>;

const contributorSchema = z.object({
  areasOfExpertise: z.string().max(500).optional().or(z.literal('')),
  technologiesKnown: z.string().max(500).optional().or(z.literal('')),
  bio: z.string().max(2000).optional().or(z.literal('')),
  availableForMentoring: z.boolean(),
});

type ContributorFormValues = z.infer<typeof contributorSchema>;

export default function UserProfilePage() {
  const { userId } = useParams<{ userId: string }>();
  const { user: currentUser } = useAuth();
  const qc = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [editingContributor, setEditingContributor] = useState(false);

  const { data: profile, isLoading, error } = useQuery({
    queryKey: ['users', userId],
    queryFn: ({ signal }) => usersApi.getUserById(userId!, signal),
    enabled: !!userId,
  });

  const { data: contributorProfile } = useQuery({
    queryKey: ['users', userId, 'contributor-profile'],
    queryFn: ({ signal }) => usersApi.getContributorProfile(userId!, signal),
    enabled: !!userId,
    retry: false,
  });

  const { data: userXp } = useQuery({
    queryKey: ['users', userId, 'xp'],
    queryFn: ({ signal }) => xpApi.getUserXp(userId!, signal),
    enabled: !!userId,
    retry: false,
  });

  const { data: userStreak } = useQuery({
    queryKey: ['users', userId, 'streak'],
    queryFn: ({ signal }) => xpApi.getUserStreak(userId!, signal),
    enabled: !!userId,
    retry: false,
  });

  const { data: endorsements } = useQuery({
    queryKey: ['users', userId, 'endorsements'],
    queryFn: ({ signal }) => xpApi.getUserEndorsements(userId!, signal),
    enabled: !!userId,
    retry: false,
  });

  usePageTitle(profile?.fullName ?? 'Profile');

  const toast = useToast();
  const isOwnProfile = currentUser?.userId === userId;

  const followMutation = useMutation({
    mutationFn: () => usersApi.followUser(userId!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['users', userId] }); toast.success('Following user.'); },
    onError: () => toast.error('Failed to follow user.'),
  });

  const unfollowMutation = useMutation({
    mutationFn: () => usersApi.unfollowUser(userId!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['users', userId] }); toast.success('Unfollowed user.'); },
    onError: () => toast.error('Failed to unfollow user.'),
  });

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty, isSubmitting },
    reset,
  } = useForm<z.input<typeof profileSchema>, unknown, ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    values: profile
      ? {
          fullName: profile.fullName,
          department: profile.department ?? '',
          designation: profile.designation ?? '',
          yearsOfExperience: profile.yearsOfExperience ?? '',
          location: profile.location ?? '',
          profilePhotoUrl: profile.profilePhotoUrl ?? '',
        }
      : undefined,
  });

  const updateMutation = useMutation({
    mutationFn: (data: ProfileFormValues) =>
      usersApi.updateUser(userId!, {
        fullName: data.fullName,
        department: data.department || undefined,
        designation: data.designation || undefined,
        yearsOfExperience: data.yearsOfExperience ? Number(data.yearsOfExperience) : undefined,
        location: data.location || undefined,
        profilePhotoUrl: data.profilePhotoUrl || undefined,
        recordVersion: profile!.recordVersion,
      }),
    onSuccess: (updated) => {
      qc.setQueryData(['users', userId], updated);
      setEditing(false);
      toast.success('Profile updated successfully.');
    },
    onError: () => toast.error('Failed to update profile.'),
  });

  const {
    register: cRegister,
    handleSubmit: cHandleSubmit,
    control: cControl,
    reset: cReset,
    formState: { errors: cErrors, isSubmitting: cIsSubmitting },
  } = useForm<ContributorFormValues>({
    resolver: zodResolver(contributorSchema),
    values: contributorProfile
      ? {
          areasOfExpertise: contributorProfile.areasOfExpertise ?? '',
          technologiesKnown: contributorProfile.technologiesKnown ?? '',
          bio: contributorProfile.bio ?? '',
          availableForMentoring: contributorProfile.availableForMentoring,
        }
      : undefined,
  });

  const updateContributorMutation = useMutation({
    mutationFn: (data: ContributorFormValues) => {
      const req: UpdateContributorProfileRequest = {
        areasOfExpertise: data.areasOfExpertise || undefined,
        technologiesKnown: data.technologiesKnown || undefined,
        bio: data.bio || undefined,
        availableForMentoring: data.availableForMentoring,
        recordVersion: contributorProfile!.recordVersion,
      };
      return usersApi.updateContributorProfile(userId!, req);
    },
    onSuccess: (updated) => {
      qc.setQueryData(['users', userId, 'contributor-profile'], updated);
      setEditingContributor(false);
      toast.success('Contributor profile updated.');
    },
    onError: () => toast.error('Failed to update contributor profile.'),
  });

  if (isLoading) return <LoadingOverlay />;
  if (!profile) return <Typography>User not found.</Typography>;

  return (
    <Box>
      <UnsavedChangesDialog when={editing && isDirty && !isSubmitting} />
      <PageHeader title={profile.fullName} />

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={updateMutation.error} />
      <ApiErrorAlert error={followMutation.error} />
      <ApiErrorAlert error={unfollowMutation.error} />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3, textAlign: 'center' }}>
            <Avatar
              src={profile.profilePhotoUrl ?? undefined}
              sx={{ width: 96, height: 96, fontSize: 40, mx: 'auto', mb: 2 }}
            >
              {profile.fullName[0]}
            </Avatar>
            <Typography variant="h6">{profile.fullName}</Typography>
            {profile.designation && (
              <Typography variant="body2" color="text.secondary">
                {profile.designation}
              </Typography>
            )}
            {profile.department && (
              <Typography variant="body2" color="text.secondary">
                {profile.department}
              </Typography>
            )}
            <Chip label={`${profile.followerCount} followers`} sx={{ mt: 1 }} size="small" />

            {!isOwnProfile && (
              <Box mt={2}>
                {profile.isFollowedByCurrentUser ? (
                  <Button
                    variant="outlined"
                    onClick={() => unfollowMutation.mutate()}
                    disabled={unfollowMutation.isPending}
                  >
                    Unfollow
                  </Button>
                ) : (
                  <Button
                    variant="contained"
                    onClick={() => followMutation.mutate()}
                    disabled={followMutation.isPending}
                  >
                    Follow
                  </Button>
                )}
              </Box>
            )}

            {isOwnProfile && !editing && (
              <Button sx={{ mt: 2 }} variant="outlined" onClick={() => setEditing(true)}>
                Edit Profile
              </Button>
            )}
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, md: 8 }}>
          {editing && isOwnProfile ? (
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                Edit Profile
              </Typography>
              <Box component="form" onSubmit={handleSubmit((d) => updateMutation.mutate(d))} noValidate>
                <TextField
                  {...register('fullName')}
                  label="Full Name"
                  fullWidth
                  error={!!errors.fullName}
                  helperText={errors.fullName?.message}
                  sx={{ mb: 2 }}
                />
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
                  <TextField
                    {...register('department')}
                    label="Department"
                    fullWidth
                    error={!!errors.department}
                    helperText={errors.department?.message as string}
                  />
                  <TextField
                    {...register('designation')}
                    label="Designation"
                    fullWidth
                    error={!!errors.designation}
                    helperText={errors.designation?.message as string}
                  />
                </Stack>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
                  <TextField
                    {...register('yearsOfExperience')}
                    label="Years of Experience"
                    type="number"
                    fullWidth
                    inputProps={{ min: 0, max: 60 }}
                    error={!!errors.yearsOfExperience}
                    helperText={errors.yearsOfExperience?.message as string}
                  />
                  <TextField
                    {...register('location')}
                    label="Location"
                    fullWidth
                    error={!!errors.location}
                    helperText={errors.location?.message as string}
                  />
                </Stack>
                <TextField
                  {...register('profilePhotoUrl')}
                  label="Profile Photo URL (optional)"
                  fullWidth
                  error={!!errors.profilePhotoUrl}
                  helperText={errors.profilePhotoUrl?.message as string}
                  sx={{ mb: 3 }}
                />
                <Stack direction="row" spacing={2}>
                  <Button type="submit" variant="contained" disabled={isSubmitting}>
                    Save Changes
                  </Button>
                  <Button onClick={() => { setEditing(false); reset(); }}>Cancel</Button>
                </Stack>
              </Box>
            </Paper>
          ) : (
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                Details
              </Typography>
              <Divider sx={{ mb: 2 }} />
              {[
                { label: 'Email', value: profile.email },
                { label: 'Department', value: profile.department },
                { label: 'Designation', value: profile.designation },
                { label: 'Location', value: profile.location },
                {
                  label: 'Experience',
                  value: profile.yearsOfExperience != null ? `${profile.yearsOfExperience} years` : undefined,
                },
              ].map(
                ({ label, value }) =>
                  value && (
                    <Box key={label} mb={1}>
                      <Typography variant="caption" color="text.secondary">
                        {label}
                      </Typography>
                      <Typography variant="body2">{value}</Typography>
                    </Box>
                  )
              )}
            </Paper>
          )}

          {/* Contributor Profile */}
          {contributorProfile && (
            <Paper sx={{ p: 3, mt: 3 }}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                <Typography variant="h6">Contributor Profile</Typography>
                {isOwnProfile && !editingContributor && (
                  <Button size="small" variant="outlined" onClick={() => setEditingContributor(true)}>Edit</Button>
                )}
              </Box>
              <Divider sx={{ mb: 2 }} />
              {editingContributor && isOwnProfile ? (
                <Box component="form" onSubmit={cHandleSubmit((d) => updateContributorMutation.mutate(d))} noValidate>
                  <ApiErrorAlert error={updateContributorMutation.error} />
                  <TextField {...cRegister('areasOfExpertise')} label="Areas of Expertise" fullWidth multiline rows={2}
                    error={!!cErrors.areasOfExpertise} helperText={cErrors.areasOfExpertise?.message as string} sx={{ mb: 2 }} />
                  <TextField {...cRegister('technologiesKnown')} label="Technologies Known" fullWidth multiline rows={2}
                    error={!!cErrors.technologiesKnown} helperText={cErrors.technologiesKnown?.message as string} sx={{ mb: 2 }} />
                  <TextField {...cRegister('bio')} label="Bio" fullWidth multiline rows={3}
                    error={!!cErrors.bio} helperText={cErrors.bio?.message as string} sx={{ mb: 2 }} />
                  <Controller name="availableForMentoring" control={cControl}
                    render={({ field }) => (
                      <FormControlLabel control={<Switch checked={field.value} onChange={(_, v) => field.onChange(v)} />}
                        label="Available for Mentoring" sx={{ mb: 2 }} />
                    )} />
                  <Stack direction="row" spacing={2}>
                    <Button type="submit" variant="contained" disabled={cIsSubmitting}>Save</Button>
                    <Button onClick={() => { setEditingContributor(false); cReset(); }}>Cancel</Button>
                  </Stack>
                </Box>
              ) : (
                <>
                  {[
                    { label: 'Areas of Expertise', value: contributorProfile.areasOfExpertise },
                    { label: 'Technologies Known', value: contributorProfile.technologiesKnown },
                    { label: 'Bio', value: contributorProfile.bio },
                  ].map(({ label, value }) => value && (
                    <Box key={label} mb={1}>
                      <Typography variant="caption" color="text.secondary">{label}</Typography>
                      <Typography variant="body2">{value}</Typography>
                    </Box>
                  ))}
                  <Stack direction="row" spacing={3} mt={1}>
                    <Box><Typography variant="caption" color="text.secondary">Avg Rating</Typography>
                      <Typography variant="body2">{contributorProfile.averageRating.toFixed(1)}</Typography></Box>
                    <Box><Typography variant="caption" color="text.secondary">Sessions Delivered</Typography>
                      <Typography variant="body2">{contributorProfile.totalSessionsDelivered}</Typography></Box>
                    <Box><Typography variant="caption" color="text.secondary">Endorsement Score</Typography>
                      <Typography variant="body2">{contributorProfile.endorsementScore.toFixed(1)}</Typography></Box>
                  </Stack>
                  <Chip label={contributorProfile.availableForMentoring ? 'Available for Mentoring' : 'Not Available for Mentoring'}
                    color={contributorProfile.availableForMentoring ? 'success' : 'default'} size="small" sx={{ mt: 1 }} />
                </>
              )}
            </Paper>
          )}

          {/* XP & Streak */}
          {(userXp || userStreak) && (
            <Paper sx={{ p: 3, mt: 3 }}>
              <Typography variant="h6" gutterBottom>XP & Streak</Typography>
              <Divider sx={{ mb: 2 }} />
              <Stack direction="row" spacing={4} flexWrap="wrap">
                {userXp && (
                  <Box>
                    <Typography variant="caption" color="text.secondary">Total XP</Typography>
                    <Typography variant="h5" fontWeight={700} color="secondary.main">{userXp.totalXp}</Typography>
                  </Box>
                )}
                {userStreak && (
                  <>
                    <Box>
                      <Typography variant="caption" color="text.secondary">Current Streak</Typography>
                      <Typography variant="h5" fontWeight={700} color="warning.main">{userStreak.currentStreakDays} days</Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">Longest Streak</Typography>
                      <Typography variant="h5" fontWeight={700}>{userStreak.longestStreakDays} days</Typography>
                    </Box>
                  </>
                )}
              </Stack>
              {userXp && userXp.recentEvents.length > 0 && (
                <Box mt={2}>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>Recent XP Events</Typography>
                  {userXp.recentEvents.slice(0, 5).map((ev, i) => (
                    <Box key={i} display="flex" justifyContent="space-between" mb={0.5}>
                      <Typography variant="body2">{ev.relatedEntityType ?? 'Activity'}</Typography>
                      <Chip label={`+${ev.xpAmount} XP`} size="small" color="secondary" variant="outlined" />
                    </Box>
                  ))}
                </Box>
              )}
            </Paper>
          )}

          {/* Endorsements Received */}
          {endorsements && endorsements.length > 0 && (
            <Paper sx={{ p: 3, mt: 3 }}>
              <Typography variant="h6" gutterBottom>Skills Endorsed</Typography>
              <Divider sx={{ mb: 2 }} />
              <Box display="flex" flexWrap="wrap" gap={1}>
                {/* Group by tag */}
                {Object.entries(
                  endorsements.reduce<Record<string, { tagName: string; count: number }>>((acc, e) => {
                    if (!acc[e.tagId]) acc[e.tagId] = { tagName: e.tagName, count: 0 };
                    acc[e.tagId].count++;
                    return acc;
                  }, {})
                ).map(([tagId, { tagName, count }]) => (
                  <Chip
                    key={tagId}
                    label={`${tagName} (${count})`}
                    color="primary"
                    variant="outlined"
                    size="small"
                  />
                ))}
              </Box>
            </Paper>
          )}
        </Grid>
      </Grid>
    </Box>
  );
}
