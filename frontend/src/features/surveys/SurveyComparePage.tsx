import { useState } from 'react';
import {
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Divider,
  LinearProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { useQuery } from '@tanstack/react-query';
import { PageHeader } from '../../shared/components/PageHeader';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { surveyAnalyticsApi } from './api/surveyAnalyticsApi';
import { surveysApi } from './api/surveysApi';
import type { SurveyDto, SurveyComparisonDto, SurveyNpsTrendDto } from './types';

export default function SurveyComparePage() {
  const [surveyA, setSurveyA] = useState<SurveyDto | null>(null);
  const [surveyB, setSurveyB] = useState<SurveyDto | null>(null);
  const [showNpsTrend, setShowNpsTrend] = useState(false);

  const { data: surveysData, isLoading: listLoading } = useQuery({
    queryKey: ['surveys', 'list'],
    queryFn: () => surveysApi.getSurveys({ pageNumber: 1, pageSize: 100 }),
  });

  const surveys: SurveyDto[] = surveysData?.data ?? [];

  const canCompare = !!surveyA && !!surveyB && surveyA.id !== surveyB.id;

  const { data: comparison, isLoading: compareLoading, error: compareError } = useQuery<SurveyComparisonDto>({
    queryKey: ['surveys', 'compare', surveyA?.id, surveyB?.id],
    queryFn: () => surveyAnalyticsApi.compareSurveys(surveyA!.id, surveyB!.id),
    enabled: canCompare,
  });

  const { data: npsTrend, isLoading: trendLoading } = useQuery<SurveyNpsTrendDto>({
    queryKey: ['surveys', 'nps-trend', surveyA?.id, surveyB?.id],
    queryFn: () => surveyAnalyticsApi.getNpsTrend([surveyA!.id, surveyB!.id]),
    enabled: canCompare && showNpsTrend,
  });

  const trendData = npsTrend?.dataPoints.map(p => ({
    name: p.surveyTitle,
    nps: p.npsScore,
    date: new Date(p.launchedAt).toLocaleDateString(),
  }));

  return (
    <Box>
      <PageHeader title="Survey Comparison" subtitle="Compare results across two surveys" />

      {/* Survey pickers */}
      <Stack direction="row" spacing={2} sx={{ mb: 4 }} flexWrap="wrap">
        <Autocomplete
          options={surveys}
          loading={listLoading}
          getOptionLabel={s => s.title}
          value={surveyA}
          onChange={(_e, val) => { setSurveyA(val); setShowNpsTrend(false); }}
          renderInput={params => <TextField {...params} label="Survey A" placeholder="Select survey…" />}
          sx={{ flex: 1, minWidth: 280 }}
          isOptionEqualToValue={(a, b) => a.id === b.id}
        />
        <Autocomplete
          options={surveys.filter(s => s.id !== surveyA?.id)}
          loading={listLoading}
          getOptionLabel={s => s.title}
          value={surveyB}
          onChange={(_e, val) => { setSurveyB(val); setShowNpsTrend(false); }}
          renderInput={params => <TextField {...params} label="Survey B" placeholder="Select survey…" />}
          sx={{ flex: 1, minWidth: 280 }}
          isOptionEqualToValue={(a, b) => a.id === b.id}
        />
      </Stack>

      {!canCompare && (
        <Typography variant="body2" color="text.secondary">
          Select two different surveys to compare their results.
        </Typography>
      )}

      {compareLoading && (
        <Box sx={{ mt: 2 }}><CircularProgress /></Box>
      )}

      {compareError && <ApiErrorAlert error={compareError} />}

      {comparison && (
        <>
          {/* Response rate bars */}
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>Response Rates</Typography>
              <Stack spacing={2}>
                {comparison.surveys.map(s => (
                  <Box key={s.surveyId}>
                    <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.5 }}>
                      <Typography variant="body2">{s.title}</Typography>
                      <Typography variant="body2" fontWeight="bold">
                        {s.responseRatePct != null ? s.responseRatePct.toFixed(1) : '0.0'}%
                      </Typography>
                    </Stack>
                    <LinearProgress
                      variant="determinate"
                      value={s.responseRatePct}
                      sx={{ height: 12, borderRadius: 6 }}
                    />
                  </Box>
                ))}
              </Stack>
            </CardContent>
          </Card>

          {/* Shared questions table */}
          {comparison.sharedQuestions.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              No shared questions found (matched by identical question text).
            </Typography>
          ) : (
            <Card sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="h6" sx={{ mb: 2 }}>Shared Questions Comparison</Typography>
                <TableContainer component={Paper} variant="outlined">
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Question</TableCell>
                        <TableCell align="center">{comparison.surveys[0]?.title ?? 'Survey A'}</TableCell>
                        <TableCell align="center">{comparison.surveys[1]?.title ?? 'Survey B'}</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {comparison.sharedQuestions.map((sq, i) => {
                        const [statA, statB] = sq.surveyStats;
                        return (
                          <TableRow key={i} hover>
                            <TableCell>{sq.questionText}</TableCell>
                            <TableCell align="center">
                              {statA
                                ? statA.averageRating !== undefined
                                  ? `Avg: ${statA.averageRating != null ? statA.averageRating.toFixed(2) : '—'}`
                                  : `${statA.totalAnswers} answers`
                                : '—'}
                            </TableCell>
                            <TableCell align="center">
                              {statB
                                ? statB.averageRating !== undefined
                                  ? `Avg: ${statB.averageRating != null ? statB.averageRating.toFixed(2) : '—'}`
                                  : `${statB.totalAnswers} answers`
                                : '—'}
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          )}

          {/* NPS Trend */}
          <Box sx={{ mb: 3 }}>
            {!showNpsTrend ? (
              <Button variant="outlined" onClick={() => setShowNpsTrend(true)}>
                View NPS Trend for These Surveys
              </Button>
            ) : (
              <Card>
                <CardContent>
                  <Typography variant="h6" sx={{ mb: 2 }}>NPS Trend</Typography>
                  {trendLoading ? (
                    <CircularProgress size={20} />
                  ) : !trendData || trendData.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      NPS data not available for these surveys.
                    </Typography>
                  ) : (
                    <ResponsiveContainer width="100%" height={240}>
                      <LineChart data={trendData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="name" />
                        <YAxis domain={[-100, 100]} />
                        <Tooltip />
                        <Legend />
                        <Line
                          type="monotone"
                          dataKey="nps"
                          stroke="#1976d2"
                          strokeWidth={2}
                          dot={{ r: 4 }}
                          name="NPS Score"
                        />
                      </LineChart>
                    </ResponsiveContainer>
                  )}
                </CardContent>
              </Card>
            )}
          </Box>
        </>
      )}

      <Divider sx={{ my: 2 }} />
    </Box>
  );
}
