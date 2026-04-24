import { useState } from 'react';
import {
  Autocomplete,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { surveyAnalyticsApi } from '../api/surveyAnalyticsApi';
import { ExportButtons } from '../components/ExportButtons';
import { NpsGauge } from '../components/NpsGauge';
import { QuestionStatsCard } from '../components/QuestionStatsCard';
import { SurveyFunnelChart } from '../components/SurveyFunnelChart';
import { SurveyHeatmapView } from '../components/SurveyHeatmapView';
import type { QuestionStatsFilters } from '../types';

interface AnalyticsTabProps {
  surveyId: string;
}

const HEALTH_COLORS: Record<string, 'success' | 'warning' | 'error'> = {
  Healthy:       'success',
  AtRisk:        'warning',
  LowEngagement: 'error',
};

export function AnalyticsTab({ surveyId }: AnalyticsTabProps) {
  const [filters, setFilters] = useState<QuestionStatsFilters>({});

  const { data: summary, isLoading: sumLoading, error: sumError } = useQuery({
    queryKey: ['surveys', surveyId, 'analytics', 'dashboard'],
    queryFn: () => surveyAnalyticsApi.getDashboard(surveyId),
  });

  const { data: questions, isLoading: qLoading, error: qError } = useQuery({
    queryKey: ['surveys', surveyId, 'analytics', 'questions', filters],
    queryFn: () => surveyAnalyticsApi.getQuestionStats(surveyId, filters),
  });

  const { data: funnel, isLoading: funnelLoading } = useQuery({
    queryKey: ['surveys', surveyId, 'analytics', 'funnel'],
    queryFn: () => surveyAnalyticsApi.getParticipationFunnel(surveyId),
  });

  const { data: nps } = useQuery({
    queryKey: ['surveys', surveyId, 'analytics', 'nps'],
    queryFn: () => surveyAnalyticsApi.getNpsReport(surveyId),
    retry: false,
  });

  const { data: heatmap, isLoading: heatmapLoading } = useQuery({
    queryKey: ['surveys', surveyId, 'analytics', 'heatmap'],
    queryFn: () => surveyAnalyticsApi.getHeatmap(surveyId),
  });

  const error = sumError ?? qError;

  if (sumLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      {/* Top Row: KPI cards + Export */}
      <Stack direction="row" spacing={2} sx={{ mb: 3 }} flexWrap="wrap" alignItems="flex-start">
        <Card sx={{ minWidth: 130 }}>
          <CardContent sx={{ textAlign: 'center' }}>
            <Typography variant="h4" color="primary.main">
              {summary != null && summary.responseRatePct != null ? `${summary.responseRatePct.toFixed(1)}%` : '—'}
            </Typography>
            <Typography variant="caption" color="text.secondary">Response Rate</Typography>
          </CardContent>
        </Card>

        <Card sx={{ minWidth: 130 }}>
          <CardContent sx={{ textAlign: 'center' }}>
            <Typography variant="h4">{summary?.totalInvited ?? '—'}</Typography>
            <Typography variant="caption" color="text.secondary">Total Invited</Typography>
          </CardContent>
        </Card>

        <Card sx={{ minWidth: 130 }}>
          <CardContent sx={{ textAlign: 'center' }}>
            <Typography variant="h4">{summary?.totalSubmitted ?? '—'}</Typography>
            <Typography variant="caption" color="text.secondary">Total Submitted</Typography>
          </CardContent>
        </Card>

        {summary && (
          <Card sx={{ minWidth: 130 }}>
            <CardContent sx={{ textAlign: 'center' }}>
              <Chip
                label={summary.healthStatus}
                color={HEALTH_COLORS[summary.healthStatus] ?? 'default'}
              />
              <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                Health
              </Typography>
            </CardContent>
          </Card>
        )}

        {nps && (
          <Card sx={{ minWidth: 130 }}>
            <CardContent sx={{ textAlign: 'center' }}>
              <NpsGauge score={nps.npsScore} />
            </CardContent>
          </Card>
        )}

        <Box sx={{ flex: 1 }} />
        <ExportButtons surveyId={surveyId} />
      </Stack>

      {/* Filter Bar */}
      <Stack direction="row" spacing={2} sx={{ mb: 3 }} flexWrap="wrap">
        <Select
          size="small"
          value={filters.department ?? ''}
          onChange={e => setFilters(f => ({ ...f, department: e.target.value || undefined }))}
          displayEmpty
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">All Departments</MenuItem>
          {/* Departments populated from analytics data */}
          {heatmap?.departments.map(d => (
            <MenuItem key={d} value={d}>{d}</MenuItem>
          ))}
        </Select>

        <Autocomplete
          size="small"
          options={[]}
          renderInput={params => <TextField {...params} placeholder="Filter by role" />}
          sx={{ minWidth: 160 }}
          onChange={(_e, val) => setFilters(f => ({ ...f, role: (val as string | null) ?? undefined }))}
        />
      </Stack>

      {/* Participation Funnel */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>Participation Funnel</Typography>
        {funnelLoading || !funnel ? (
          <CircularProgress size={20} />
        ) : (
          <SurveyFunnelChart data={funnel} />
        )}
      </Box>

      <Divider sx={{ my: 3 }} />

      {/* NPS Section */}
      {nps && (
        <>
          <Box sx={{ mb: 4 }}>
            <Typography variant="h6" sx={{ mb: 2 }}>NPS Breakdown</Typography>
            <Stack direction="row" spacing={3} alignItems="center" flexWrap="wrap">
              <NpsGauge score={nps.npsScore} />
              <Stack spacing={1}>
                <Typography variant="body2">
                  <strong>Promoters:</strong> {nps.promoters} ({nps.promoterPct != null ? nps.promoterPct.toFixed(1) : '0.0'}%)
                </Typography>
                <Typography variant="body2">
                  <strong>Passives:</strong> {nps.passives} ({nps.passivePct != null ? nps.passivePct.toFixed(1) : '0.0'}%)
                </Typography>
                <Typography variant="body2">
                  <strong>Detractors:</strong> {nps.detractors} ({nps.detractorPct != null ? nps.detractorPct.toFixed(1) : '0.0'}%)
                </Typography>
              </Stack>
            </Stack>
          </Box>
          <Divider sx={{ my: 3 }} />
        </>
      )}

      {/* Per-question stats */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>Per-Question Statistics</Typography>
        {qLoading ? (
          <CircularProgress size={20} />
        ) : (questions ?? []).length === 0 ? (
          <Typography variant="body2" color="text.secondary">No responses yet.</Typography>
        ) : (
          (questions ?? []).map(q => (
            <QuestionStatsCard key={q.questionId} question={q} />
          ))
        )}
      </Box>

      <Divider sx={{ my: 3 }} />

      {/* Department Heatmap */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>Department Heatmap</Typography>
        {heatmapLoading || !heatmap ? (
          <CircularProgress size={20} />
        ) : heatmap.departments.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No rating-type questions found for heatmap.
          </Typography>
        ) : (
          <SurveyHeatmapView data={heatmap} />
        )}
      </Box>
    </Box>
  );
}
