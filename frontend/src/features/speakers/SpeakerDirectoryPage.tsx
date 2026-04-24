import {
  Autocomplete,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Grid,
  Rating,
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { speakersApi } from '../../shared/api/speakers';
import { usersApi } from '../../shared/api/users';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import type { UserDto } from '../../shared/types';
import { Link } from 'react-router-dom';
import { useToast } from '../../shared/hooks/useToast';

interface ProfileFormState {
  areasOfExpertise: string;
  technologiesKnown: string;
  bio: string;
  availableForMentoring: boolean;
}

const emptyForm: ProfileFormState = {
  areasOfExpertise: '',
  technologiesKnown: '',
  bio: '',
  availableForMentoring: false,
};

export default function SpeakerDirectoryPage() {
  usePageTitle('Speakers');
  const { hasRole } = useAuth();
  const qc = useQueryClient();

  const [search, setSearch] = useState('');
  const [expertise, setExpertise] = useState('');
  const [page, setPage] = useState(1);

  // Add Speaker Profile dialog state
  const [dialogOpen, setDialogOpen] = useState(false);
  const [userSearch, setUserSearch] = useState('');
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [profileForm, setProfileForm] = useState<ProfileFormState>(emptyForm);

  const canManage =
    hasRole(UserRole.KnowledgeTeam) || hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);

  const { data, isLoading, error } = useQuery({
    queryKey: ['speakers', search, expertise, page],
    queryFn: ({ signal }) =>
      speakersApi.getSpeakers({
        searchTerm: search || undefined,
        expertiseArea: expertise || undefined,
        pageNumber: page,
        pageSize: 12,
      }, signal),
  });

  const toast = useToast();
  const { data: usersData } = useQuery({
    queryKey: ['users-search', userSearch],
    queryFn: ({ signal }) => usersApi.getUsers({ search: userSearch, pageSize: 20, pageNumber: 1 }, signal),
    enabled: dialogOpen,
  });

  const addProfileMutation = useMutation({
    mutationFn: () =>
      usersApi.updateContributorProfile(selectedUser!.id, {
        areasOfExpertise: profileForm.areasOfExpertise || undefined,
        technologiesKnown: profileForm.technologiesKnown || undefined,
        bio: profileForm.bio || undefined,
        availableForMentoring: profileForm.availableForMentoring,
        recordVersion: 0,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['speakers'] });
      setDialogOpen(false);
      setSelectedUser(null);
      setProfileForm(emptyForm);
      setUserSearch('');
      toast.success('Speaker profile added.');
    },
    onError: () => toast.error('Failed to add speaker profile.'),
  });

  const handleDialogClose = () => {
    setDialogOpen(false);
    setSelectedUser(null);
    setProfileForm(emptyForm);
    setUserSearch('');
  };

  return (
    <Box>
      <PageHeader
        title="Speaker Directory"
        subtitle="Discover knowledge contributors and subject matter experts"
        actions={
          canManage ? (
            <Button variant="contained" onClick={() => setDialogOpen(true)}>
              Add Speaker Profile
            </Button>
          ) : undefined
        }
      />

      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 240 }}
        />
        <TextField
          size="small"
          label="Expertise"
          value={expertise}
          onChange={(e) => { setExpertise(e.target.value); setPage(1); }}
          sx={{ minWidth: 200 }}
        />
      </Box>

      <ApiErrorAlert error={error} />

      {isLoading ? (
        <LoadingOverlay />
      ) : (
        <>
          <Grid container spacing={3}>
            {data?.data.map((speaker) => (
              <Grid size={{ xs: 12, sm: 6, md: 4 }} key={speaker.userId}>
                <Card sx={{ height: '100%' }}>
                  <CardContent>
                    <Box display="flex" alignItems="center" gap={2} mb={1}>
                      <Avatar src={speaker.profilePhotoUrl ?? undefined} sx={{ width: 52, height: 52 }}>
                        {speaker.fullName[0]}
                      </Avatar>
                      <Box>
                        <Typography
                          variant="subtitle1"
                          fontWeight={600}
                          component={Link}
                          to={`/speakers/${speaker.userId}`}
                          sx={{ textDecoration: 'none', color: 'inherit' }}
                        >
                          {speaker.fullName}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {speaker.designation}
                          {speaker.department ? ` · ${speaker.department}` : ''}
                        </Typography>
                      </Box>
                    </Box>

                    <Stack direction="row" spacing={1} mb={1} alignItems="center">
                      <Rating value={speaker.averageRating} precision={0.5} size="small" readOnly />
                      <Typography variant="caption" color="text.secondary">
                        ({speaker.totalSessionsDelivered} sessions)
                      </Typography>
                    </Stack>

                    {speaker.areasOfExpertise && (
                      <Typography variant="body2" color="text.secondary" mb={1}>
                        {speaker.areasOfExpertise}
                      </Typography>
                    )}

                    <Stack direction="row" spacing={1} flexWrap="wrap">
                      <Chip label={`${speaker.followerCount} followers`} size="small" variant="outlined" />
                      {speaker.availableForMentoring && (
                        <Chip label="Open to mentoring" size="small" color="success" variant="outlined" />
                      )}
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          {data && data.data.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={6}>
              No speakers found.
            </Typography>
          )}

          {data && data.totalPages > 1 && (
            <Box display="flex" justifyContent="center" mt={4} gap={1}>
              <Button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <Chip label={`${page} / ${data.totalPages}`} />
              <Button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </Box>
          )}
        </>
      )}

      {/* Add Speaker Profile Dialog */}
      <Dialog open={dialogOpen} onClose={handleDialogClose} fullWidth maxWidth="sm">
        <DialogTitle>Add Speaker Profile</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} mt={1}>
            <ApiErrorAlert error={addProfileMutation.error} />

            <Autocomplete
              options={usersData?.data ?? []}
              getOptionLabel={(u) => `${u.fullName} (${u.email})`}
              value={selectedUser}
              onChange={(_, value) => setSelectedUser(value)}
              inputValue={userSearch}
              onInputChange={(_, value) => setUserSearch(value)}
              renderInput={(params) => (
                <TextField {...params} label="Select User" required placeholder="Type to search users..." />
              )}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              noOptionsText="No users found"
            />

            <TextField
              label="Areas of Expertise"
              fullWidth
              value={profileForm.areasOfExpertise}
              onChange={(e) => setProfileForm((f) => ({ ...f, areasOfExpertise: e.target.value }))}
              placeholder="e.g. React, System Design, DevOps"
            />

            <TextField
              label="Technologies Known"
              fullWidth
              value={profileForm.technologiesKnown}
              onChange={(e) => setProfileForm((f) => ({ ...f, technologiesKnown: e.target.value }))}
              placeholder="e.g. TypeScript, Kubernetes, PostgreSQL"
            />

            <TextField
              label="Bio"
              fullWidth
              multiline
              rows={3}
              value={profileForm.bio}
              onChange={(e) => setProfileForm((f) => ({ ...f, bio: e.target.value }))}
            />

            <FormControlLabel
              control={
                <Checkbox
                  checked={profileForm.availableForMentoring}
                  onChange={(e) =>
                    setProfileForm((f) => ({ ...f, availableForMentoring: e.target.checked }))
                  }
                />
              }
              label="Available for mentoring"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleDialogClose}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!selectedUser || addProfileMutation.isPending}
            onClick={() => addProfileMutation.mutate()}
          >
            Create Profile
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
