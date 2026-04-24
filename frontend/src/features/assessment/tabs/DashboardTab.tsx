import {
  Box,
  Card,
  CardContent,
  Chip,
  Stack,
  Typography,
  LinearProgress,
} from '@/components/ui';
import { useQuery } from '@tanstack/react-query';
import { assessmentDashboardApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import type { RatingBandCount } from '../types';

function StatCard({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
  return (
    <Card sx={{ flex: 1, minWidth: 150 }}>
      <CardContent>
        <Typography variant="h4" fontWeight={700} color="primary">{value}</Typography>
        <Typography variant="body2" color="text.secondary">{label}</Typography>
        {sub && <Typography variant="caption" color="text.secondary">{sub}</Typography>}
      </CardContent>
    </Card>
  );
}

function RatingBar({ dist }: { dist: RatingBandCount[] }) {
  const total = dist.reduce((s, r) => s + r.count, 0);
  if (total === 0) return <Typography variant="body2" color="text.secondary">No data</Typography>;
  return (
    <Stack spacing={1}>
      {dist.map((r) => (
        <Box key={r.ratingName}>
          <Stack direction="row" justifyContent="space-between">
            <Typography variant="body2">{r.ratingName} ({r.numericValue})</Typography>
            <Typography variant="body2" fontWeight={600}>{r.count}</Typography>
          </Stack>
          <LinearProgress
            variant="determinate"
            value={(r.count / total) * 100}
            sx={{ height: 8, borderRadius: 4 }}
          />
        </Box>
      ))}
    </Stack>
  );
}

export function DashboardTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'dashboard', 'admin'],
    queryFn: () => assessmentDashboardApi.getAdminDashboard(),
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Typography variant="h6" mb={1}>{data.periodName}</Typography>
      <Stack direction="row" spacing={2} flexWrap="wrap" mb={3}>
        <StatCard label="Total Groups"    value={data.totalGroups} />
        <StatCard label="Total Employees" value={data.totalEmployees} />
        <StatCard label="Avg Completion"  value={`${data.avgCompletionPercent}%`} />
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography variant="h6" mb={2}>Rating Distribution</Typography>
            <RatingBar dist={data.ratingDistribution} />
          </CardContent>
        </Card>

        <Card sx={{ flex: 2 }}>
          <CardContent>
            <Typography variant="h6" mb={2}>Group Completions</Typography>
            <Stack spacing={1}>
              {data.groupCompletions.map((g) => (
                <Box key={g.groupId}>
                  <Stack direction="row" justifyContent="space-between" mb={0.5}>
                    <Typography variant="body2" fontWeight={600}>{g.groupName}</Typography>
                    <Chip
                      label={`${g.completionPercent}%`}
                      size="small"
                      color={g.completionPercent === 100 ? 'success' : g.completionPercent > 50 ? 'warning' : 'error'}
                    />
                  </Stack>
                  <LinearProgress
                    variant="determinate"
                    value={g.completionPercent}
                    sx={{ height: 6, borderRadius: 3 }}
                  />
                </Box>
              ))}
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
}
