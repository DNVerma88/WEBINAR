import {
  AddIcon,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { mentoringApi } from '../../shared/api/mentoring';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { UserAutocomplete } from '../../shared/components/UserAutocomplete';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { MentorMenteeStatus } from '../../shared/types';
import type { UserDto } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';

const statusLabel: Record<MentorMenteeStatus, string> = {
  [MentorMenteeStatus.Pending]: 'Pending',
  [MentorMenteeStatus.Active]: 'Active',
  [MentorMenteeStatus.Completed]: 'Completed',
  [MentorMenteeStatus.Declined]: 'Declined',
};

const statusColor: Record<MentorMenteeStatus, 'default' | 'warning' | 'success' | 'error'> = {
  [MentorMenteeStatus.Pending]: 'warning',
  [MentorMenteeStatus.Active]: 'success',
  [MentorMenteeStatus.Completed]: 'default',
  [MentorMenteeStatus.Declined]: 'error',
};

export default function MentoringPage() {
  usePageTitle('Mentoring');
  const { user } = useAuth();
  const qc = useQueryClient();
  const [showRequest, setShowRequest] = useState(false);
  const [selectedMentor, setSelectedMentor] = useState<UserDto | null>(null);
  const [goalsText, setGoalsText] = useState('');

  const toast = useToast();
  const { data, isLoading, error } = useQuery({
    queryKey: ['mentoring'],
    queryFn: ({ signal }) => mentoringApi.getPairings(signal),
  });

  const requestMutation = useMutation({
    mutationFn: () => mentoringApi.requestMentor({ mentorId: selectedMentor!.id, goalsText: goalsText || undefined }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['mentoring'] });
      setShowRequest(false);
      setSelectedMentor(null);
      setGoalsText('');
      toast.success('Mentor request sent.');
    },
    onError: () => toast.error('Failed to send mentor request.'),
  });

  const acceptMutation = useMutation({
    mutationFn: (id: string) => mentoringApi.accept(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['mentoring'] }); toast.success('Request accepted.'); },
    onError: () => toast.error('Failed to accept request.'),
  });

  const declineMutation = useMutation({
    mutationFn: (id: string) => mentoringApi.decline(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['mentoring'] }); toast.success('Request declined.'); },
    onError: () => toast.error('Failed to decline request.'),
  });

  return (
    <Box>
      <PageHeader
        title="Mentoring"
        subtitle="Connect with mentors and grow your expertise"
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowRequest(true)}>
            Request a Mentor
          </Button>
        }
      />

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      <Stack spacing={2}>
        {data?.data.map((pairing) => (
          <Card key={pairing.id}>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="flex-start" flexWrap="wrap" gap={2}>
                <Box>
                  <Typography variant="h6" sx={{ fontSize: '1rem' }}>
                    {pairing.mentorName}{' '}
                    <Typography component="span" variant="body2" color="text.secondary">
                      mentoring
                    </Typography>{' '}
                    {pairing.menteeName}
                  </Typography>
                  {pairing.goalsText && (
                    <Typography variant="body2" color="text.secondary" mt={0.5}>
                      Goals: {pairing.goalsText}
                    </Typography>
                  )}
                  {pairing.startedAt && (
                    <Typography variant="caption" color="text.secondary">
                      Started {new Date(pairing.startedAt).toLocaleDateString()}
                    </Typography>
                  )}
                </Box>
                <Box display="flex" gap={1} alignItems="center">
                  <Chip
                    label={statusLabel[pairing.status]}
                    color={statusColor[pairing.status]}
                    size="small"
                  />
                  {pairing.status === MentorMenteeStatus.Pending && pairing.mentorId === user?.userId && (
                    <>
                      <Button
                        size="small"
                        variant="contained"
                        color="success"
                        onClick={() => acceptMutation.mutate(pairing.id)}
                        disabled={acceptMutation.isPending}
                      >
                        Accept
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        color="error"
                        onClick={() => declineMutation.mutate(pairing.id)}
                        disabled={declineMutation.isPending}
                      >
                        Decline
                      </Button>
                    </>
                  )}
                </Box>
              </Box>
            </CardContent>
          </Card>
        ))}
        {data?.data.length === 0 && (
          <Typography color="text.secondary" textAlign="center" py={4}>
            No mentoring relationships yet.
          </Typography>
        )}
      </Stack>

      <Dialog open={showRequest} onClose={() => setShowRequest(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Request a Mentor</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <UserAutocomplete
              value={selectedMentor}
              onChange={setSelectedMentor}
              label="Mentor"
              required
              fullWidth
              helperText="Search for a mentor by name"
            />
            <TextField
              label="Your Goals"
              value={goalsText}
              onChange={(e) => setGoalsText(e.target.value)}
              fullWidth
              multiline
              rows={3}
              helperText="What do you hope to achieve with this mentoring relationship?"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowRequest(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => requestMutation.mutate()}
            disabled={!selectedMentor || requestMutation.isPending}
          >
            Send Request
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
