import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
  CalendarIcon,
  PersonIcon,
} from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { speakerMarketplaceApi } from '../../shared/api/speaker-marketplace';
import { sessionsApi } from '../../shared/api/sessions';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import type { SpeakerAvailabilityDto, BookingStatus, SetAvailabilityRequest, SpeakerBookingDto } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';

// ── Helpers ──────────────────────────────────────────────────────────────────
function bookingStatusColor(s: BookingStatus) {
  return s === 'Accepted' ? 'success'
    : s === 'Declined' ? 'error'
      : s === 'Completed' ? 'default'
        : s === 'Cancelled' ? 'default'
          : 'warning';
}

// ── Book speaker dialog ───────────────────────────────────────────────────────
function BookSpeakerDialog({ slot, onClose }: { slot: SpeakerAvailabilityDto; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [sessionId, setSessionId] = useState('');
  const { register, handleSubmit } = useForm<{ topic: string; description: string }>({
    defaultValues: { topic: slot.topics[0] ?? '', description: '' },
  });

  const { data: sessions } = useQuery({
    queryKey: ['sessions', 'all'],
    queryFn: ({ signal }) => sessionsApi.getSessions({ pageSize: 200 }, signal),
  });

  const mutation = useMutation({
    mutationFn: (d: { topic: string; description: string }) =>
      speakerMarketplaceApi.requestBooking(slot.id, {
        topic: d.topic,
        description: d.description || undefined,
        intendedSessionId: sessionId || undefined,
      }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['speaker-marketplace'] }); onClose(); toast.success('Booking request sent.'); },
    onError: () => toast.error('Failed to request booking.'),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Request Booking — {slot.speakerName}</DialogTitle>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <Box>
              <Typography variant="body2" color="text.secondary">Available</Typography>
              <Typography>
                {new Date(slot.availableFrom).toLocaleString()} — {new Date(slot.availableTo).toLocaleString()}
              </Typography>
            </Box>
            {slot.topics.length > 0 && (
              <Box>
                <Typography variant="body2" color="text.secondary" mb={0.5}>Speaker topics</Typography>
                <Stack direction="row" spacing={0.5} flexWrap="wrap">
                  {slot.topics.map((t) => <Chip key={t} label={t} size="small" />)}
                </Stack>
              </Box>
            )}
            <TextField {...register('topic', { required: true })} label="Your Topic" fullWidth required />
            <TextField {...register('description')} label="Description" multiline rows={2} fullWidth />
            <FormControl fullWidth>
              <InputLabel>For Session (optional)</InputLabel>
              <Select
                value={sessionId}
                label="For Session (optional)"
                onChange={(e) => setSessionId(e.target.value)}
              >
                <MenuItem value=""><em>No specific session yet</em></MenuItem>
                {(sessions?.data ?? []).map((s) => (
                  <MenuItem key={s.id} value={s.id}>
                    {s.title} — {new Date(s.scheduledAt).toLocaleDateString()}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            {mutation.error && <ApiErrorAlert error={mutation.error} />}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>Request Booking</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── Link to session dialog ────────────────────────────────────────────────────
function LinkToSessionDialog({ booking, onClose }: { booking: SpeakerBookingDto; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [sessionId, setSessionId] = useState('');

  const { data: sessions, isLoading } = useQuery({
    queryKey: ['sessions', 'scheduled'],
    queryFn: ({ signal }) => sessionsApi.getSessions({ pageSize: 100 }, signal),
  });

  const mutation = useMutation({
    mutationFn: () => speakerMarketplaceApi.linkToSession(booking.id, sessionId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['speaker-marketplace', 'bookings'] });
      onClose();
      toast.success('Booking linked to session.');
    },
    onError: () => toast.error('Failed to link booking.'),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Link Booking to Session</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          <Box>
            <Typography variant="body2" color="text.secondary">Booking</Typography>
            <Typography fontWeight={500}>{booking.topic}</Typography>
            <Typography variant="body2" color="text.secondary">
              Speaker: {booking.speakerName}
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary">
            Linking will assign <strong>{booking.speakerName}</strong> as the speaker for the selected session.
          </Typography>
          {isLoading ? (
            <LoadingOverlay />
          ) : (
            <FormControl fullWidth required>
              <InputLabel>Select Session</InputLabel>
              <Select
                value={sessionId}
                label="Select Session"
                onChange={(e) => setSessionId(e.target.value)}
              >
                {(sessions?.data ?? []).map((s) => (
                  <MenuItem key={s.id} value={s.id}>
                    {s.title} — {new Date(s.scheduledAt).toLocaleDateString()}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
          {mutation.error && <ApiErrorAlert error={mutation.error} />}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={!sessionId || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          Link to Session
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Admin direct-assign dialog ────────────────────────────────────────────────
function AdminAssignDialog({ slot, onClose }: { slot: SpeakerAvailabilityDto; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [sessionId, setSessionId] = useState('');
  const { register, handleSubmit } = useForm<{ topic: string; description: string }>(
    { defaultValues: { topic: slot.topics[0] ?? '', description: '' } }
  );

  const { data: sessions, isLoading: sessionsLoading } = useQuery({
    queryKey: ['sessions', 'all'],
    queryFn: ({ signal }) => sessionsApi.getSessions({ pageSize: 200 }, signal),
  });

  const mutation = useMutation({
    mutationFn: (d: { topic: string; description: string }) =>
      speakerMarketplaceApi.adminAssign({
        speakerAvailabilityId: slot.id,
        sessionId,
        topic: d.topic,
        description: d.description || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['speaker-marketplace'] });
      qc.invalidateQueries({ queryKey: ['sessions'] });
      onClose();
      toast.success('Speaker assigned to session.');
    },
    onError: () => toast.error('Failed to assign speaker.'),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Assign Speaker to Session</DialogTitle>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <Box>
              <Typography variant="body2" color="text.secondary">Speaker</Typography>
              <Typography fontWeight={500}>{slot.speakerName}</Typography>
              <Typography variant="body2" color="text.secondary">
                Available: {new Date(slot.availableFrom).toLocaleString()} — {new Date(slot.availableTo).toLocaleString()}
              </Typography>
            </Box>
            <TextField
              {...register('topic', { required: true })}
              label="Topic / Session Purpose"
              fullWidth
              required
            />
            <TextField
              {...register('description')}
              label="Notes (optional)"
              multiline
              rows={2}
              fullWidth
            />
            {sessionsLoading ? <LoadingOverlay /> : (
              <FormControl fullWidth required>
                <InputLabel>Session to assign speaker to</InputLabel>
                <Select
                  value={sessionId}
                  label="Session to assign speaker to"
                  onChange={(e) => setSessionId(e.target.value)}
                >
                  {(sessions?.data ?? []).map((s) => (
                    <MenuItem key={s.id} value={s.id}>
                      {s.title} — {new Date(s.scheduledAt).toLocaleDateString()}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
            {mutation.error && <ApiErrorAlert error={mutation.error} />}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button
            type="submit"
            variant="contained"
            disabled={!sessionId || mutation.isPending}
          >
            Assign Speaker
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── Browse speakers tab ───────────────────────────────────────────────────────
function BrowseSpeakersTab() {
  const [topicFilter, setTopicFilter] = useState('');
  const [bookSlot, setBookSlot] = useState<SpeakerAvailabilityDto | null>(null);
  const [assignSlot, setAssignSlot] = useState<SpeakerAvailabilityDto | null>(null);
  const { hasRole } = useAuth();
  const isAdminOrKt = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin) || hasRole(UserRole.KnowledgeTeam);

  const { data, isLoading, error } = useQuery({
    queryKey: ['speaker-marketplace', 'available', topicFilter],
    queryFn: ({ signal }) => speakerMarketplaceApi.getAvailableSpeakers({
      topic: topicFilter || undefined,
      pageSize: 50,
    }, signal),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" spacing={2} mb={3} alignItems="center">
        <TextField
          size="small"
          label="Filter by topic"
          value={topicFilter}
          onChange={(e) => setTopicFilter(e.target.value)}
          sx={{ minWidth: 220 }}
        />
        <Typography variant="body2" color="text.secondary">
          {data?.totalCount ?? 0} slots available
        </Typography>
      </Stack>

      <Stack spacing={2}>
        {data?.data.map((slot) => (
          <Card key={slot.id} variant="outlined">
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box flex={1}>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <PersonIcon fontSize="small" color="action" />
                    <Typography fontWeight={600}>{slot.speakerName}</Typography>
                    {slot.isRecurring && <Chip label="Recurring" size="small" color="primary" variant="outlined" />}
                  </Stack>
                  <Stack direction="row" spacing={0.5} alignItems="center" mb={1}>
                    <CalendarIcon fontSize="small" color="action" />
                    <Typography variant="body2">
                      {new Date(slot.availableFrom).toLocaleString()} — {new Date(slot.availableTo).toLocaleString()}
                    </Typography>
                  </Stack>
                  {slot.topics.length > 0 && (
                    <Stack direction="row" spacing={0.5} flexWrap="wrap">
                      {slot.topics.map((t) => <Chip key={t} label={t} size="small" />)}
                    </Stack>
                  )}
                  {slot.notes && (
                    <Typography variant="caption" color="text.secondary" display="block" mt={0.5}>
                      {slot.notes}
                    </Typography>
                  )}
                </Box>
                <Button
                  size="small"
                  variant="contained"
                  disabled={slot.isBooked}
                  onClick={() => setBookSlot(slot)}
                >
                  {slot.isBooked ? 'Booked' : 'Request Booking'}
                </Button>
                {isAdminOrKt && !slot.isBooked && (
                  <Button
                    size="small"
                    variant="outlined"
                    color="secondary"
                    onClick={() => setAssignSlot(slot)}
                  >
                    Assign to Session
                  </Button>
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
        {data?.data.length === 0 && (
          <Typography color="text.secondary">No speakers available with the selected filters.</Typography>
        )}
      </Stack>

      {bookSlot && <BookSpeakerDialog slot={bookSlot} onClose={() => setBookSlot(null)} />}
      {assignSlot && <AdminAssignDialog slot={assignSlot} onClose={() => setAssignSlot(null)} />}
    </Box>
  );
}

// ── Set availability dialog ───────────────────────────────────────────────────
function SetAvailabilityDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const { register, handleSubmit, reset } = useForm<SetAvailabilityRequest>({
    defaultValues: { availableFrom: '', availableTo: '', isRecurring: false, topics: [], notes: '' },
  });

  const mutation = useMutation({
    mutationFn: (d: SetAvailabilityRequest) => speakerMarketplaceApi.setAvailability(d),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['speaker-marketplace', 'mine'] }); reset(); onClose(); toast.success('Availability added.'); },
    onError: () => toast.error('Failed to set availability.'),
  });

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add Availability</DialogTitle>
      <form onSubmit={handleSubmit((d) => mutation.mutate({ ...d, topics: [] }))}>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <TextField {...register('availableFrom', { required: true })}
              label="Available From" type="datetime-local" fullWidth required InputLabelProps={{ shrink: true }} />
            <TextField {...register('availableTo', { required: true })}
              label="Available To" type="datetime-local" fullWidth required InputLabelProps={{ shrink: true }} />
            <TextField {...register('notes')} label="Notes" multiline rows={2} fullWidth />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>Save</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── My availability tab ───────────────────────────────────────────────────────
function MyAvailabilityTab() {
  const [addOpen, setAddOpen] = useState(false);
  const qc = useQueryClient();
  const toast = useToast();

  const { data, isLoading, error } = useQuery({
    queryKey: ['speaker-marketplace', 'mine'],
    queryFn: ({ signal }) => speakerMarketplaceApi.getMyAvailability(signal),
  });

  const deleteMutation = useMutation({
    mutationFn: speakerMarketplaceApi.deleteAvailability,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['speaker-marketplace', 'mine'] }); toast.success('Availability removed.'); },
    onError: () => toast.error('Failed to remove availability.'),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" justifyContent="flex-end" mb={2}>
        <Button variant="contained" onClick={() => setAddOpen(true)}>Add Availability</Button>
      </Stack>
      <Stack spacing={1.5}>
        {(data ?? []).map((slot) => (
          <Card key={slot.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box>
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <CalendarIcon fontSize="small" color="action" />
                    <Typography variant="body2">
                      {new Date(slot.availableFrom).toLocaleString()} — {new Date(slot.availableTo).toLocaleString()}
                    </Typography>
                  </Stack>
                  {slot.isBooked && <Chip label="Booked" size="small" color="warning" sx={{ mt: 0.5 }} />}
                </Box>
                {!slot.isBooked && (
                  <Button size="small" color="error" onClick={() => deleteMutation.mutate(slot.id)}>
                    Remove
                  </Button>
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
        {(data ?? []).length === 0 && (
          <Typography color="text.secondary">No availability slots set.</Typography>
        )}
      </Stack>
      <SetAvailabilityDialog open={addOpen} onClose={() => setAddOpen(false)} />
    </Box>
  );
}

// ── My bookings tab ───────────────────────────────────────────────────────────
function MyBookingsTab() {
  const [asSpeaker, setAsSpeaker] = useState(false);
  const [linkBooking, setLinkBooking] = useState<SpeakerBookingDto | null>(null);
  const qc = useQueryClient();
  const toast = useToast();

  const { data, isLoading, error } = useQuery({
    queryKey: ['speaker-marketplace', 'bookings', asSpeaker],
    queryFn: ({ signal }) => speakerMarketplaceApi.getMyBookings({ asSpeaker, pageSize: 50 }, signal),
  });

  const respondMutation = useMutation({
    mutationFn: ({ id, isAccepted }: { id: string; isAccepted: boolean }) =>
      speakerMarketplaceApi.respondToBooking(id, { isAccepted }),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ['speaker-marketplace', 'bookings'] });
      toast.success(vars.isAccepted ? 'Booking accepted.' : 'Booking declined.');
    },
    onError: () => toast.error('Failed to respond to booking.'),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" spacing={1} mb={3}>
        <Button variant={!asSpeaker ? 'contained' : 'outlined'} onClick={() => setAsSpeaker(false)} size="small">
          As Requester
        </Button>
        <Button variant={asSpeaker ? 'contained' : 'outlined'} onClick={() => setAsSpeaker(true)} size="small">
          As Speaker
        </Button>
      </Stack>
      <Stack spacing={1.5}>
        {data?.data.map((booking) => (
          <Card key={booking.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box flex={1}>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <Typography fontWeight={600}>{booking.topic}</Typography>
                    <Chip label={booking.status} size="small" color={bookingStatusColor(booking.status)} />
                    {booking.linkedSessionId && (
                      <Chip label="Session Linked" size="small" color="info" variant="outlined" />
                    )}
                  </Stack>
                  <Typography variant="body2" color="text.secondary">
                    {asSpeaker
                      ? `Requested by ${booking.requesterName}`
                      : `Speaker: ${booking.speakerName}`}
                  </Typography>
                  {booking.description && (
                    <Typography variant="caption" color="text.secondary" display="block">
                      {booking.description}
                    </Typography>
                  )}
                </Box>
                <Stack direction="row" spacing={1} alignItems="center">
                  {asSpeaker && booking.status === 'Pending' && (
                    <>
                      <Button size="small" color="success"
                        onClick={() => respondMutation.mutate({ id: booking.id, isAccepted: true })}>
                        Accept
                      </Button>
                      <Button size="small" color="error"
                        onClick={() => respondMutation.mutate({ id: booking.id, isAccepted: false })}>
                        Decline
                      </Button>
                    </>
                  )}
                  {booking.status === 'Accepted' && !booking.linkedSessionId && (
                    <Button size="small" variant="outlined" onClick={() => setLinkBooking(booking)}>
                      Link to Session
                    </Button>
                  )}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
        {data?.data.length === 0 && (
          <Typography color="text.secondary">No bookings found.</Typography>
        )}
      </Stack>
      {linkBooking && <LinkToSessionDialog booking={linkBooking} onClose={() => setLinkBooking(null)} />}
    </Box>
  );
}

// ── Main page ────────────────────────────────────────────────────────────────
export default function SpeakerMarketplacePage() {
  usePageTitle('Speaker Marketplace');
  const [tab, setTab] = useState(0);

  return (
    <Box>
      <PageHeader
        title="Speaker Marketplace"
        subtitle="Browse available speakers or share your own availability"
      />
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tab} onChange={(_, v) => setTab(v)}>
          <Tab label="Browse Speakers" />
          <Tab label="My Availability" />
          <Tab label="My Bookings" />
        </Tabs>
      </Box>
      {tab === 0 && <BrowseSpeakersTab />}
      {tab === 1 && <MyAvailabilityTab />}
      {tab === 2 && <MyBookingsTab />}
    </Box>
  );
}
