import {
  Box,
  Card,
  CardContent,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { leaderboardsApi } from '../../shared/api/leaderboards';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import type { LeaderboardType } from '../../shared/types';

const leaderboardTypes: { value: LeaderboardType; label: string }[] = [
  { value: 'ByXp', label: 'By XP' },
  { value: 'ByContributions', label: 'By Contributions' },
  { value: 'ByAttendance', label: 'By Attendance' },
  { value: 'ByRating', label: 'By Rating' },
  { value: 'ByMentoring', label: 'By Mentoring' },
  { value: 'ByDepartment', label: 'By Department' },
];

const medalColors = ['#FFD700', '#C0C0C0', '#CD7F32'] as const;

export default function LeaderboardPage() {
  usePageTitle('Leaderboard');
  const [type, setType] = useState<LeaderboardType>('ByXp');

  const { data, isLoading, error } = useQuery({
    queryKey: ['leaderboard', type],
    queryFn: ({ signal }) => leaderboardsApi.getLeaderboard(type, undefined, undefined, signal),
    staleTime: 5 * 60_000,
  });

  return (
    <Box>
      <PageHeader
        title="Leaderboard"
        subtitle="See who's leading the pack in knowledge sharing"
      />

      <Box mb={3}>
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel>Category</InputLabel>
          <Select
            value={type}
            label="Category"
            onChange={(e) => setType(e.target.value as LeaderboardType)}
          >
            {leaderboardTypes.map((t) => (
              <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      <Stack spacing={1}>
        {data?.entries.map((entry) => (
          <Card
            key={entry.userId}
            sx={{
              borderLeft: entry.rank <= 3 ? `4px solid ${medalColors[entry.rank - 1] ?? 'transparent'}` : undefined,
            }}
          >
            <CardContent sx={{ py: '12px !important' }}>
              <Box display="flex" alignItems="center" gap={2}>
                <Typography
                  variant="h5"
                  fontWeight={700}
                  sx={{ minWidth: 36, color: entry.rank <= 3 ? medalColors[entry.rank - 1] : 'text.secondary' }}
                >
                  #{entry.rank}
                </Typography>
                <Box flexGrow={1}>
                  <Typography variant="body1" fontWeight={600}>
                    {entry.displayName}
                  </Typography>
                </Box>
                <Chip
                  label={`${Math.round(entry.score).toLocaleString()} pts`}
                  color={entry.rank === 1 ? 'warning' : 'default'}
                  variant={entry.rank <= 3 ? 'filled' : 'outlined'}
                />
              </Box>
            </CardContent>
          </Card>
        ))}
        {data?.entries.length === 0 && (
          <Typography color="text.secondary" textAlign="center" py={4}>
            No leaderboard data available yet.
          </Typography>
        )}
      </Stack>
    </Box>
  );
}
