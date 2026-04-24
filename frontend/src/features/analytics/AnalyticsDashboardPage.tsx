import {
  Box,
  Card,
  CardContent,
  Chip,
  Stack,
  Tab,
  Tabs,
  Typography,
  LinearProgress,
} from '@/components/ui';
import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../../shared/api/analytics';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';

// ── Simple stat card ────────────────────────────────────────────────────────
function StatCard({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
  return (
    <Card sx={{ flex: 1, minWidth: 150 }}>
      <CardContent>
        <Typography variant="h4" fontWeight={700} color="primary">
          {value}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
        {sub && (
          <Typography variant="caption" color="text.secondary">
            {sub}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

// ── Tab panels ──────────────────────────────────────────────────────────────
function SummaryTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'summary'],
    queryFn: ({ signal }) => analyticsApi.getSummary(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Stack direction="row" spacing={2} flexWrap="wrap" mb={3}>
        <StatCard label="Total Sessions" value={data.totalSessions} />
        <StatCard label="Knowledge Assets" value={data.totalAssets} />
        <StatCard label="Total Users" value={data.totalUsers} />
        <StatCard label="Avg Quiz Pass Rate" value={`${data.avgQuizPassRate}%`} />
        <StatCard label="Weekly Active Users" value={data.weeklyActiveUsers} />
      </Stack>

      <Typography variant="h6" mb={2}>Top Categories</Typography>
      <Stack spacing={1}>
        {data.topCategories.map((cat) => (
          <Card key={cat.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography fontWeight={600}>{cat.name}</Typography>
                <Stack direction="row" spacing={1}>
                  <Chip label={`${cat.sessionCount} sessions`} size="small" />
                  <Chip label={`${cat.assetCount} assets`} size="small" color="primary" variant="outlined" />
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
}

function HeatmapTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'heatmap'],
    queryFn: ({ signal }) => analyticsApi.getKnowledgeGapHeatmap(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box sx={{ overflowX: 'auto' }}>
      <Typography variant="subtitle2" color="text.secondary" mb={2}>
        Engagement score per category × department (0–100)
      </Typography>
      <Box component="table" sx={{ borderCollapse: 'collapse', width: '100%' }}>
        <Box component="thead">
          <Box component="tr">
            <Box component="th" sx={{ p: 1, textAlign: 'left', fontWeight: 600 }}>Dept \ Category</Box>
            {data.categories.map((cat) => (
              <Box component="th" key={cat} sx={{ p: 1, textAlign: 'center', fontWeight: 600, fontSize: 12 }}>
                {cat}
              </Box>
            ))}
          </Box>
        </Box>
        <Box component="tbody">
          {data.departments.map((dept) => (
            <Box component="tr" key={dept}>
              <Box component="td" sx={{ p: 1, fontWeight: 500, whiteSpace: 'nowrap' }}>{dept}</Box>
              {data.categories.map((cat) => {
                // FE-12: cellLookup built once with useMemo instead of O(n³) .find() on every render
                const cell = cellLookup[`${dept}::${cat}`];
                const score = cell?.engagementScore ?? 0;
                const bg = score > 70 ? '#4caf50' : score > 40 ? '#ff9800' : '#f44336';
                return (
                  <Box component="td" key={cat} sx={{ p: 1, textAlign: 'center' }}>
                    <Box sx={{
                      bgcolor: bg, color: '#fff', borderRadius: 1, px: 1, py: 0.5,
                      fontSize: 12, fontWeight: 600, display: 'inline-block', minWidth: 40,
                    }}>
                      {score.toFixed(0)}
                    </Box>
                  </Box>
                );
              })}
            </Box>
          ))}
        </Box>
      </Box>
    </Box>
  );
}

function SkillCoverageTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'skill-coverage'],
    queryFn: ({ signal }) => analyticsApi.getSkillCoverage(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Stack direction="row" alignItems="center" spacing={2} mb={3}>
        <Typography variant="h6">Overall Coverage</Typography>
        <Chip label={`${data.overallCoveragePercent}%`} color="primary" />
      </Stack>
      <Stack spacing={2}>
        {data.categories.map((cat) => (
          <Card key={cat.categoryId} variant="outlined">
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
                <Typography fontWeight={600}>{cat.categoryName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {cat.coveredTagCount}/{cat.totalTagCount} skills
                </Typography>
              </Stack>
              <LinearProgress
                variant="determinate"
                value={cat.coveragePercent}
                sx={{ height: 8, borderRadius: 4, mb: 1 }}
              />
              {cat.gapSkills.length > 0 && (
                <Box>
                  <Typography variant="caption" color="text.secondary">Gaps: </Typography>
                  {cat.gapSkills.map((s) => (
                    <Chip key={s} label={s} size="small" sx={{ mr: 0.5, mt: 0.5 }} color="warning" variant="outlined" />
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
}

function FreshnessTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'freshness'],
    queryFn: ({ signal }) => analyticsApi.getContentFreshness(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  const buckets = [
    { label: '< 30 days (Fresh)', count: data.freshCount, color: 'success' as const },
    { label: '30–90 days (Recent)', count: data.recentCount, color: 'primary' as const },
    { label: '90–180 days (Aging)', count: data.agingCount, color: 'warning' as const },
    { label: '> 180 days (Stale)', count: data.staleCount, color: 'error' as const },
  ];

  return (
    <Box>
      <Stack direction="row" spacing={2} flexWrap="wrap" mb={3}>
        {buckets.map((b) => (
          <StatCard key={b.label} label={b.label} value={b.count} />
        ))}
      </Stack>
      <Typography variant="h6" mb={2}>Stalest Content</Typography>
      <Stack spacing={1}>
        {data.stalestItems.map((item) => (
          <Card key={item.id} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography fontWeight={500}>{item.title}</Typography>
                  <Typography variant="caption" color="text.secondary">{item.contentType}</Typography>
                </Box>
                <Chip label={`${item.ageDays} days old`} size="small" color="error" variant="outlined" />
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
}

function FunnelTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'funnel'],
    queryFn: ({ signal }) => analyticsApi.getLearningFunnel(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  const steps = [
    { label: 'Discovered', value: data.discovered, rate: null },
    { label: 'Registered', value: data.registered, rate: data.registrationRate },
    { label: 'Attended', value: data.attended, rate: data.attendanceRate },
    { label: 'Rated', value: data.rated, rate: data.ratingRate },
    { label: 'Quiz Passed', value: data.quizPassed, rate: data.passRate },
  ];

  return (
    <Stack spacing={1.5} maxWidth={500}>
      {steps.map((step, i) => (
        <Box key={step.label}>
          <Stack direction="row" justifyContent="space-between" mb={0.5}>
            <Typography variant="body2" fontWeight={500}>{step.label}</Typography>
            <Stack direction="row" spacing={1} alignItems="center">
              <Typography variant="body2">{step.value.toLocaleString()}</Typography>
              {step.rate !== null && (
                <Chip label={`${step.rate}%`} size="small" color={step.rate > 50 ? 'success' : 'warning'} />
              )}
            </Stack>
          </Stack>
          <LinearProgress
            variant="determinate"
            value={data.discovered > 0 ? (step.value / data.discovered) * 100 : 0}
            sx={{ height: 20, borderRadius: 2, opacity: 1 - i * 0.07 }}
          />
        </Box>
      ))}
    </Stack>
  );
}

function CohortTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'cohort'],
    queryFn: ({ signal }) => analyticsApi.getCohortCompletionRates(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Stack spacing={2}>
      {data.items.map((item) => (
        <Card key={item.learningPathId} variant="outlined">
          <CardContent>
            <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
              <Typography fontWeight={600}>{item.title}</Typography>
              <Stack direction="row" spacing={1}>
                <Chip label={`${item.completionRate}% completed`}
                  color={item.completionRate >= 70 ? 'success' : item.completionRate >= 40 ? 'warning' : 'error'}
                  size="small" />
                <Chip label={`${item.totalEnrollments} enrolled`} size="small" variant="outlined" />
              </Stack>
            </Stack>
            <LinearProgress variant="determinate" value={item.completionRate} sx={{ height: 8, borderRadius: 4 }} />
            <Typography variant="caption" color="text.secondary" mt={0.5} display="block">
              Avg completion: {item.avgCompletionDays.toFixed(0)} days
            </Typography>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}

function DeptEngagementTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'dept-engagement'],
    queryFn: ({ signal }) => analyticsApi.getDepartmentEngagement(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  // FE-13: memoize the sorted array — re-sorting on every render allocates a new
  // array and runs O(n log n) comparisons even when the data hasn't changed
  // eslint-disable-next-line react-hooks/rules-of-hooks
  const sorted = useMemo(
    () => [...data.departments].sort((a, b) => b.engagementScore - a.engagementScore),
    [data.departments]
  );

  return (
    <Stack spacing={1.5}>
      {sorted.map((dept) => (
        <Card key={dept.department} variant="outlined">
          <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" mb={0.5}>
              <Typography fontWeight={600}>{dept.department}</Typography>
              <Chip label={`Score: ${dept.engagementScore.toFixed(0)}`}
                color={dept.engagementScore >= 70 ? 'success' : dept.engagementScore >= 40 ? 'warning' : 'default'}
                size="small" />
            </Stack>
            <Stack direction="row" spacing={2}>
              <Typography variant="caption" color="text.secondary">{dept.sessionsAttended} attended</Typography>
              <Typography variant="caption" color="text.secondary">{dept.assetsCreated} assets</Typography>
              <Typography variant="caption" color="text.secondary">{dept.totalXpEarned.toLocaleString()} XP</Typography>
            </Stack>
            <LinearProgress
              variant="determinate"
              value={dept.engagementScore}
              sx={{ height: 6, borderRadius: 3, mt: 0.5 }}
            />
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}

function RetentionTab() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['analytics', 'retention'],
    queryFn: ({ signal }) => analyticsApi.getKnowledgeRetention(signal),
    staleTime: 10 * 60_000,
  });

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Stack direction="row" alignItems="center" spacing={2} mb={3}>
        <Typography variant="h6">Overall Retention Score</Typography>
        <Chip label={`${data.overallRetentionScore.toFixed(0)}%`} color="primary" />
      </Stack>
      <Stack spacing={1.5}>
        {data.categories.map((cat) => (
          <Card key={cat.categoryId} variant="outlined">
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mb={0.5}>
                <Typography fontWeight={600}>{cat.categoryName}</Typography>
                <Chip label={`${cat.retentionScore.toFixed(0)}%`}
                  color={cat.retentionScore >= 70 ? 'success' : cat.retentionScore >= 40 ? 'warning' : 'error'}
                  size="small" />
              </Stack>
              <Stack direction="row" spacing={2}>
                <Typography variant="caption" color="text.secondary">{cat.quizAttempts} attempts</Typography>
                <Typography variant="caption" color="text.secondary">Pass rate: {cat.passRate.toFixed(0)}%</Typography>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
}

// ── Main page ────────────────────────────────────────────────────────────────
const TABS = [
  { label: 'Summary', component: SummaryTab },
  { label: 'Knowledge Gap', component: HeatmapTab },
  { label: 'Skill Coverage', component: SkillCoverageTab },
  { label: 'Content Freshness', component: FreshnessTab },
  { label: 'Learning Funnel', component: FunnelTab },
  { label: 'Cohort Completion', component: CohortTab },
  { label: 'Dept Engagement', component: DeptEngagementTab },
  { label: 'Retention', component: RetentionTab },
];

export default function AnalyticsDashboardPage() {
  usePageTitle('Analytics Dashboard');
  const [tab, setTab] = useState(0);
  const TabComponent = TABS[tab].component;

  return (
    <Box>
      <PageHeader title="Analytics Dashboard" subtitle="Org-wide learning insights and performance metrics" />
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" scrollButtons="auto">
          {TABS.map((t) => <Tab key={t.label} label={t.label} />)}
        </Tabs>
      </Box>
      <TabComponent />
    </Box>
  );
}
