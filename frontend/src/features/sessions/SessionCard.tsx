import { Card, CardActionArea, CardContent, Chip, Stack, Typography } from '@/components/ui';
import { memo } from 'react';
import { Link } from 'react-router-dom';
import type { SessionDto } from '../../shared/types';
import { SessionFormat, SessionStatus, DifficultyLevel, SESSION_FORMAT_LABELS, SESSION_STATUS_LABELS, DIFFICULTY_LEVEL_LABELS } from '../../shared/types';

const statusColor: Record<SessionStatus, 'default' | 'warning' | 'success' | 'error'> = {
  [SessionStatus.Scheduled]: 'warning',
  [SessionStatus.InProgress]: 'success',
  [SessionStatus.Completed]: 'default',
  [SessionStatus.Cancelled]: 'error',
};

const difficultyColor: Record<DifficultyLevel, 'success' | 'warning' | 'error'> = {
  [DifficultyLevel.Beginner]: 'success',
  [DifficultyLevel.Intermediate]: 'warning',
  [DifficultyLevel.Advanced]: 'error',
};

interface SessionCardProps {
  session: SessionDto;
}

// FE-08: React.memo prevents all 12 grid cards from re-rendering on every filter
// state change (search, category, status, etc.) when the session data itself hasn't changed
export const SessionCard = memo(function SessionCard({ session }: SessionCardProps) {
  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardActionArea component={Link} to={`/sessions/${session.id}`} sx={{ flexGrow: 1 }}>
        <CardContent>
          <Stack direction="row" spacing={1} mb={1} flexWrap="wrap">
            <Chip
              label={SESSION_STATUS_LABELS[session.status as SessionStatus] ?? session.status}
              color={statusColor[session.status]}
              size="small"
            />
            <Chip
              label={DIFFICULTY_LEVEL_LABELS[session.difficultyLevel as DifficultyLevel] ?? session.difficultyLevel}
              color={difficultyColor[session.difficultyLevel]}
              size="small"
              variant="outlined"
            />
          </Stack>
          <Typography variant="h6" gutterBottom sx={{ lineHeight: 1.3 }}>
            {session.title}
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            {session.speakerName} · {session.categoryName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {SESSION_FORMAT_LABELS[session.format as SessionFormat] ?? session.format} · {session.durationMinutes} min
          </Typography>
          {session.scheduledAt && (
            <Typography variant="body2" color="text.secondary" mt={0.5}>
              {new Date(session.scheduledAt).toLocaleString()}
            </Typography>
          )}
          <Typography variant="caption" color="text.secondary" display="block" mt={1}>
            {session.registeredCount} registered
            {session.participantLimit ? ` / ${session.participantLimit} max` : ''}
            {session.waitlistCount ? ` · ${session.waitlistCount} waitlisted` : ''}
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
});
