import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@/components/ui';
import { TablePagination } from '@mui/material';
import { useState } from 'react';
import {
  BarChart, Bar, LineChart, Line, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts';
import { useQuery } from '@tanstack/react-query';
import {
  assessmentReportApi,
  assessmentPeriodApi,
  assessmentGroupApi,
} from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';

type ReportType = 'completion' | 'detailed' | 'risk' | 'trend' | 'group-dist' | 'role-dist' | 'improvement' | 'work-role-rating';

const REPORT_TYPES: { value: ReportType; label: string }[] = [
  { value: 'completion',   label: 'Completion Report' },
  { value: 'detailed',     label: 'Detailed Report' },
  { value: 'risk',         label: 'Risk Report' },
  { value: 'trend',        label: 'Trend Report' },
  { value: 'group-dist',   label: 'Group Distribution' },
  { value: 'role-dist',         label: 'Role Distribution' },
  { value: 'improvement',       label: 'Improvement Report' },
  { value: 'work-role-rating',  label: 'Work Role Rating' },
];

const CHART_COLORS = ['#1976d2','#2e7d32','#ed6c02','#9c27b0','#0288d1','#c62828','#558b2f','#6a1b9a'];

export function ReportsTab() {
  const [reportType, setReportType] = useState<ReportType>('completion');
  const [periodId, setPeriodId]   = useState('');
  const [groupId, setGroupId]     = useState('');
  const [fromPeriodId, setFromPeriodId] = useState('');
  const [toPeriodId, setToPeriodId]     = useState('');
  const [fromYear, setFromYear] = useState(new Date().getFullYear() - 1);
  const [toYear, setToYear]     = useState(new Date().getFullYear());
  const [detailPage, setDetailPage] = useState(0);
  const [detailRows, setDetailRows] = useState(25);

  // Lookups
  const { data: periods } = useQuery({
    queryKey: ['assessment', 'periods-list'],
    queryFn: () => assessmentPeriodApi.getPeriods({ pageNumber: 1, pageSize: 200 }),
    select: (d) => d.data,
  });
  const { data: groups } = useQuery({
    queryKey: ['assessment', 'groups-list'],
    queryFn: () => assessmentGroupApi.getGroups({ pageNumber: 1, pageSize: 200 }),
    select: (d) => d.data,
  });

  // Report queries (each only fires when its type is active and required params are set)
  const { data: completion, isLoading: l1, error: e1 } = useQuery({
    queryKey: ['assessment', 'report', 'completion', periodId, groupId],
    queryFn: () => assessmentReportApi.getCompletionReport(periodId || undefined, groupId || undefined),
    enabled: reportType === 'completion',
  });

  const { data: detailed, isLoading: l2, error: e2 } = useQuery({
    queryKey: ['assessment', 'report', 'detailed', periodId, groupId, detailPage, detailRows],
    queryFn: () => assessmentReportApi.getDetailedReport({
      periodId: periodId || undefined, groupId: groupId || undefined,
      pageNumber: detailPage + 1, pageSize: detailRows,
    }),
    enabled: reportType === 'detailed',
  });

  const { data: risk, isLoading: l3, error: e3 } = useQuery({
    queryKey: ['assessment', 'report', 'risk', periodId],
    queryFn: () => assessmentReportApi.getRiskReport(periodId),
    enabled: reportType === 'risk' && !!periodId,
  });

  const { data: trend, isLoading: l4, error: e4 } = useQuery({
    queryKey: ['assessment', 'report', 'trend', fromYear, toYear, groupId],
    queryFn: () => assessmentReportApi.getTrendReport({ fromYear, toYear, groupId: groupId || undefined }),
    enabled: reportType === 'trend',
  });

  const { data: groupDist, isLoading: l5, error: e5 } = useQuery({
    queryKey: ['assessment', 'report', 'group-dist', periodId],
    queryFn: () => assessmentReportApi.getGroupDistribution(periodId),
    enabled: reportType === 'group-dist' && !!periodId,
  });

  const { data: roleDist, isLoading: l6, error: e6 } = useQuery({
    queryKey: ['assessment', 'report', 'role-dist', periodId],
    queryFn: () => assessmentReportApi.getRoleDistribution(periodId),
    enabled: reportType === 'role-dist' && !!periodId,
  });

  const { data: improvement, isLoading: l7, error: e7 } = useQuery({
    queryKey: ['assessment', 'report', 'improvement', fromPeriodId, toPeriodId],
    queryFn: () => assessmentReportApi.getImprovementReport({ fromPeriodId: fromPeriodId as never, toPeriodId: toPeriodId as never }),
    enabled: reportType === 'improvement' && !!fromPeriodId && !!toPeriodId,
  });

  const { data: workRoleRating, isLoading: l8, error: e8 } = useQuery({
    queryKey: ['assessment', 'report', 'work-role-rating', periodId, groupId],
    queryFn: () => assessmentReportApi.getWorkRoleRatingReport(periodId || undefined, groupId || undefined),
    enabled: reportType === 'work-role-rating',
  });

  const isLoading = l1 || l2 || l3 || l4 || l5 || l6 || l7 || l8;
  const error     = e1 || e2 || e3 || e4 || e5 || e6 || e7 || e8;

  const handleExport = async () => {
    const blob = await assessmentReportApi.exportCsv({ periodId: periodId || undefined, groupId: groupId || undefined });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'ai-assessment-export.csv'; a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Box>
      {/* ── Toolbar ── */}
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={3} alignItems="center" flexWrap="wrap">
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel>Report Type</InputLabel>
          <Select label="Report Type" value={reportType}
            onChange={(e) => setReportType(e.target.value as ReportType)}>
            {REPORT_TYPES.map(r => <MenuItem key={r.value} value={r.value}>{r.label}</MenuItem>)}
          </Select>
        </FormControl>

        {/* Period filter */}
        {['completion', 'detailed', 'risk', 'group-dist', 'role-dist', 'work-role-rating'].includes(reportType) && (
          <FormControl size="small" sx={{ minWidth: 220 }}>
            <InputLabel>Period</InputLabel>
            <Select label="Period" value={periodId} onChange={(e) => setPeriodId(e.target.value as string)}>
              <MenuItem value="">All Periods</MenuItem>
              {periods?.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
            </Select>
          </FormControl>
        )}

        {/* Group filter */}
        {['completion', 'detailed', 'trend'].includes(reportType) && (
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Group</InputLabel>
            <Select label="Group" value={groupId} onChange={(e) => setGroupId(e.target.value as string)}>
              <MenuItem value="">All Groups</MenuItem>
              {groups?.map(g => <MenuItem key={g.id} value={g.id}>{g.groupName}</MenuItem>)}
            </Select>
          </FormControl>
        )}

        {/* Trend year range */}
        {reportType === 'trend' && (
          <>
            <TextField label="From Year" type="number" size="small" value={fromYear} sx={{ width: 110 }}
              onChange={(e) => setFromYear(+e.target.value)} />
            <TextField label="To Year" type="number" size="small" value={toYear} sx={{ width: 110 }}
              onChange={(e) => setToYear(+e.target.value)} />
          </>
        )}

        {/* Improvement period range */}
        {reportType === 'improvement' && (
          <>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>From Period</InputLabel>
              <Select label="From Period" value={fromPeriodId} onChange={(e) => setFromPeriodId(e.target.value as string)}>
                <MenuItem value="">— select —</MenuItem>
                {periods?.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>To Period</InputLabel>
              <Select label="To Period" value={toPeriodId} onChange={(e) => setToPeriodId(e.target.value as string)}>
                <MenuItem value="">— select —</MenuItem>
                {periods?.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
              </Select>
            </FormControl>
          </>
        )}

        <Button variant="outlined" size="small" onClick={handleExport} sx={{ ml: 'auto' }}>Export CSV</Button>
      </Stack>

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      {/* ── Completion Report ── */}
      {reportType === 'completion' && completion && (
        <Box>
          <Stack direction="row" spacing={2} flexWrap="wrap" mb={3}>
            {[
              { label: 'Total Expected', value: completion.totalExpected, color: 'primary.main' },
              { label: 'Completed',      value: completion.completed,     color: 'success.main' },
              { label: 'Pending',        value: completion.pending,       color: 'warning.main' },
              { label: 'Completion %',   value: `${completion.completionPct}%`, color: 'info.main' },
            ].map(stat => (
              <Card key={stat.label} sx={{ flex: 1, minWidth: 140 }}>
                <CardContent>
                  <Typography variant="h4" fontWeight={700} color={stat.color}>{stat.value}</Typography>
                  <Typography variant="body2" color="text.secondary">{stat.label}</Typography>
                </CardContent>
              </Card>
            ))}
          </Stack>

          {/* Completion bar chart by group */}
          {completion.byGroup.length > 0 && (
            <Card variant="outlined" sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Completion % by Group</Typography>
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={completion.byGroup.map(g => ({ name: g.groupName, 'Completion %': g.completionPercent, Submitted: g.submitted, Total: g.totalEmployees }))}
                    margin={{ top: 5, right: 20, left: 0, bottom: 60 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" angle={-35} textAnchor="end" tick={{ fontSize: 12 }} />
                    <YAxis domain={[0, 100]} unit="%" />
                    <Tooltip formatter={(v, n) => [n === 'Completion %' ? `${v}%` : v, n]} />
                    <Legend verticalAlign="top" />
                    <Bar dataKey="Completion %" fill="#1976d2" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="Submitted"   fill="#2e7d32" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="Total"       fill="#ed6c02" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          <Typography variant="h6" mb={1}>By Group</Typography>
          <Stack spacing={1}>
            {completion.byGroup.map((g) => (
              <Card key={g.groupId} variant="outlined">
                <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Box>
                      <Typography fontWeight={600}>{g.groupName}</Typography>
                      <Typography variant="caption" color="text.secondary">Champion: {g.primaryLeadName}</Typography>
                    </Box>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Typography variant="body2">{g.submitted}/{g.totalEmployees}</Typography>
                      <Chip label={`${g.completionPercent}%`} size="small"
                        color={g.completionPercent === 100 ? 'success' : g.completionPercent > 50 ? 'warning' : 'error'} />
                    </Stack>
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Stack>
        </Box>
      )}

      {/* ── Detailed Report ── */}
      {reportType === 'detailed' && detailed && (
        <>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Employee</TableCell>
                  <TableCell>Designation</TableCell>
                  <TableCell>Group</TableCell>
                  <TableCell>Period</TableCell>
                  <TableCell>Current Rating</TableCell>
                  <TableCell>Change</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {detailed.data.map((r, i) => (
                  <TableRow key={i} hover>
                    <TableCell>
                      <Typography variant="body2" fontWeight={600}>{r.employeeName}</Typography>
                      <Typography variant="caption" color="text.secondary">{r.department}</Typography>
                    </TableCell>
                    <TableCell>{r.designation}</TableCell>
                    <TableCell>{r.groupName}</TableCell>
                    <TableCell>{r.periodName}</TableCell>
                    <TableCell>
                      <Chip label={`${r.currentRatingValue} – ${r.currentRatingName}`} size="small" color="primary" />
                    </TableCell>
                    <TableCell>
                      {r.ratingChange != null && (
                        <Chip label={r.ratingChange > 0 ? `+${r.ratingChange}` : String(r.ratingChange)}
                          size="small" color={r.ratingChange > 0 ? 'success' : r.ratingChange < 0 ? 'error' : 'default'} />
                      )}
                    </TableCell>
                    <TableCell><Chip label={r.status} size="small" /></TableCell>
                  </TableRow>
                ))}
                {detailed.data.length === 0 && (
                  <TableRow><TableCell colSpan={7} align="center">No records found.</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination component="div" count={detailed.totalCount}
            page={detailPage} onPageChange={(_, p) => setDetailPage(p)}
            rowsPerPage={detailRows}
            onRowsPerPageChange={(e) => { setDetailRows(+e.target.value); setDetailPage(0); }}
            rowsPerPageOptions={[10, 25, 50]} />
        </>
      )}

      {/* ── Risk Report ── */}
      {reportType === 'risk' && risk && (
        <Stack spacing={1}>
          {risk.length === 0 && <Typography color="text.secondary">No risk data — select a period first.</Typography>}
          {risk.map((r, i) => (
            <Card key={i} variant="outlined">
              <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Box>
                    <Typography fontWeight={600}>{r.groupName}</Typography>
                    <Typography variant="caption" color="text.secondary">Avg Score: {r.avgScore}</Typography>
                  </Box>
                  <Stack direction="row" spacing={1}>
                    {r.missing > 0 && <Chip label={`${r.missing} missing`} size="small" color="error" />}
                    <Chip label={r.riskLevel} size="small" color={r.riskLevel === 'High' ? 'error' : r.riskLevel === 'Medium' ? 'warning' : 'success'} />
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          ))}
        </Stack>
      )}

      {/* ── Trend Report ── */}
      {reportType === 'trend' && trend && (
        <Box>
          <Stack direction="row" spacing={2} mb={2}>
            {[
              { label: 'Improving', value: trend.improvingCount, color: 'success.main' },
              { label: 'Declining', value: trend.decliningCount, color: 'error.main' },
              { label: 'Stable',    value: trend.stableCount,    color: 'info.main' },
            ].map(s => (
              <Card key={s.label} sx={{ minWidth: 120 }}>
                <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                  <Typography variant="h5" fontWeight={700} color={s.color}>{s.value}</Typography>
                  <Typography variant="caption">{s.label}</Typography>
                </CardContent>
              </Card>
            ))}
          </Stack>

          {/* Trend line chart */}
          {trend.periods.length > 0 && (
            <Card variant="outlined" sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Average Score Over Time</Typography>
                <ResponsiveContainer width="100%" height={260}>
                  <LineChart data={trend.periods.map(p => ({ name: p.periodName, 'Avg Score': p.avgScore, 'Total Rated': p.totalRated }))}
                    margin={{ top: 5, right: 20, left: 0, bottom: 60 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" angle={-35} textAnchor="end" tick={{ fontSize: 12 }} />
                    <YAxis yAxisId="score" />
                    <YAxis yAxisId="count" orientation="right" />
                    <Tooltip />
                    <Legend verticalAlign="top" />
                    <Line yAxisId="score" type="monotone" dataKey="Avg Score"  stroke="#1976d2" strokeWidth={2} dot={{ r: 4 }} />
                    <Line yAxisId="count" type="monotone" dataKey="Total Rated" stroke="#2e7d32" strokeWidth={2} dot={{ r: 4 }} strokeDasharray="5 5" />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Period</TableCell>
                  <TableCell align="right">Avg Score</TableCell>
                  <TableCell align="right">Total Rated</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {trend.periods.map((p, i) => (
                  <TableRow key={i} hover>
                    <TableCell>{p.periodName}</TableCell>
                    <TableCell align="right">{p.avgScore}</TableCell>
                    <TableCell align="right">{p.totalRated}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}

      {/* ── Group Distribution ── */}
      {reportType === 'group-dist' && groupDist && (
        <Box>
          {groupDist.length === 0 && <Typography color="text.secondary">Select a period to view group distribution.</Typography>}

          {/* Stacked bar chart — all groups side by side */}
          {groupDist.length > 0 && (() => {
            // Collect all unique rating names for bars
            const ratingNames = Array.from(new Set(groupDist.flatMap(g => g.distribution.map(d => d.ratingName))));
            const chartData = groupDist.map(g => {
              const row: Record<string, string | number> = { name: g.groupName };
              g.distribution.forEach(d => { row[d.ratingName] = d.count; });
              return row;
            });
            return (
              <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                  <Typography variant="subtitle1" fontWeight={600} mb={2}>Rating Distribution by Group</Typography>
                  <ResponsiveContainer width="100%" height={280}>
                    <BarChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 60 }}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" angle={-35} textAnchor="end" tick={{ fontSize: 12 }} />
                      <YAxis allowDecimals={false} />
                      <Tooltip />
                      <Legend verticalAlign="top" />
                      {ratingNames.map((name, idx) => (
                        <Bar key={name} dataKey={name} stackId="a" fill={CHART_COLORS[idx % CHART_COLORS.length]} radius={idx === ratingNames.length - 1 ? [4,4,0,0] : [0,0,0,0]} />
                      ))}
                    </BarChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            );
          })()}

          <Stack spacing={2}>
            {groupDist.map((g, i) => (
              <Card key={i} variant="outlined">
                <CardContent>
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="flex-start">
                    <Box sx={{ flex: 1 }}>
                      <Typography fontWeight={600} mb={1}>{g.groupName}</Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        {g.distribution.map((d) => (
                          <Chip key={d.ratingName} label={`${d.ratingName}: ${d.count}`} size="small" color="primary" variant="outlined" />
                        ))}
                      </Stack>
                    </Box>
                    {g.distribution.length > 0 && (
                      <Box sx={{ width: 200, height: 160 }}>
                        <ResponsiveContainer width="100%" height="100%">
                          <PieChart>
                            <Pie data={g.distribution.map(d => ({ name: d.ratingName, value: d.count }))}
                              cx="50%" cy="50%" outerRadius={65} dataKey="value">
                              {g.distribution.map((_, idx) => (
                                <Cell key={idx} fill={CHART_COLORS[idx % CHART_COLORS.length]} />
                              ))}
                            </Pie>
                            <Tooltip formatter={(v, n) => [v, n]} />
                          </PieChart>
                        </ResponsiveContainer>
                      </Box>
                    )}
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Stack>
        </Box>
      )}

      {/* ── Role Distribution ── */}
      {reportType === 'role-dist' && roleDist && (
        <Box>
          {/* Avg score bar chart by designation */}
          {roleDist.length > 0 && (
            <Card variant="outlined" sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Average Score by Designation</Typography>
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={roleDist.map(r => ({ name: r.designation, 'Avg Score': r.avgScore }))}
                    margin={{ top: 5, right: 20, left: 0, bottom: 60 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" angle={-35} textAnchor="end" tick={{ fontSize: 12 }} />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="Avg Score" fill="#1976d2" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Designation</TableCell>
                  <TableCell align="right">Avg Score</TableCell>
                  <TableCell>Distribution</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {roleDist.length === 0 && (
                  <TableRow><TableCell colSpan={3} align="center">Select a period to view role distribution.</TableCell></TableRow>
                )}
                {roleDist.map((r, i) => (
                  <TableRow key={i} hover>
                    <TableCell>{r.designation}</TableCell>
                    <TableCell align="right">{r.avgScore}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} flexWrap="wrap">
                        {r.distribution.map(d => (
                          <Chip key={d.ratingName} label={`${d.ratingName}: ${d.count}`} size="small" variant="outlined" />
                        ))}
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}

      {/* ── Improvement Report ── */}
      {reportType === 'improvement' && improvement && (
        <Box>
          {/* Score change bar chart */}
          {improvement.length > 0 && (
            <Card variant="outlined" sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Rating Change per Employee</Typography>
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={improvement.map(r => ({ name: r.employeeName, 'Change': r.change, 'From': r.fromRating, 'To': r.toRating }))}
                    margin={{ top: 5, right: 20, left: 0, bottom: 80 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" angle={-40} textAnchor="end" tick={{ fontSize: 11 }} />
                    <YAxis />
                    <Tooltip />
                    <Legend verticalAlign="top" />
                    <Bar dataKey="From"   fill="#ed6c02" radius={[4,4,0,0]} />
                    <Bar dataKey="To"     fill="#1976d2" radius={[4,4,0,0]} />
                    <Bar dataKey="Change" radius={[4,4,0,0]}>
                      {improvement.map((r, idx) => (
                        <Cell key={idx} fill={r.change > 0 ? '#2e7d32' : r.change < 0 ? '#c62828' : '#9e9e9e'} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Employee</TableCell>
                  <TableCell>Group</TableCell>
                  <TableCell align="right">From Rating</TableCell>
                  <TableCell align="right">To Rating</TableCell>
                  <TableCell align="right">Change</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {improvement.length === 0 && (
                  <TableRow><TableCell colSpan={5} align="center">Select from/to periods to view improvements.</TableCell></TableRow>
                )}
                {improvement.map((r, i) => (
                  <TableRow key={i} hover>
                    <TableCell>{r.employeeName}</TableCell>
                    <TableCell>{r.groupName}</TableCell>
                    <TableCell align="right">{r.fromRating}</TableCell>
                    <TableCell align="right">{r.toRating}</TableCell>
                    <TableCell align="right">
                      <Chip label={r.change > 0 ? `+${r.change}` : String(r.change)} size="small"
                        color={r.change > 0 ? 'success' : r.change < 0 ? 'error' : 'default'} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}

      {/* ── Work Role Rating Report ── */}
      {reportType === 'work-role-rating' && workRoleRating && (
        <Box>
          {workRoleRating.length === 0 && (
            <Typography color="text.secondary" variant="body2">No data — make sure group members have Work Roles assigned and assessments are submitted.</Typography>
          )}
          {workRoleRating.length > 0 && (
            <Card variant="outlined" sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Average Score per Work Role</Typography>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart
                    layout="vertical"
                    data={workRoleRating.map(r => ({ name: r.workRoleName, 'Avg Score': r.avgScore, category: r.category }))}
                    margin={{ top: 5, right: 30, left: 160, bottom: 5 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" domain={[0, 5]} />
                    <YAxis type="category" dataKey="name" tick={{ fontSize: 11 }} width={150} />
                    <Tooltip />
                    <Bar dataKey="Avg Score" radius={[0,4,4,0]}>
                      {workRoleRating.map((_, idx) => (
                        <Cell key={idx} fill={CHART_COLORS[idx % CHART_COLORS.length]} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Code</TableCell>
                  <TableCell>Work Role</TableCell>
                  <TableCell>Category</TableCell>
                  <TableCell align="right">Rated</TableCell>
                  <TableCell align="right">Avg Score</TableCell>
                  <TableCell>Distribution</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {workRoleRating.map((r, i) => (
                  <TableRow key={i} hover>
                    <TableCell><Chip label={r.workRoleCode} size="small" variant="outlined" /></TableCell>
                    <TableCell>{r.workRoleName}</TableCell>
                    <TableCell>
                      <Chip label={r.category} size="small"
                        color="primary" variant="outlined" />
                    </TableCell>
                    <TableCell align="right">{r.totalRated}</TableCell>
                    <TableCell align="right">
                      <Chip
                        label={r.avgScore.toFixed(2)}
                        size="small"
                        color={r.avgScore >= 4 ? 'success' : r.avgScore >= 2.5 ? 'warning' : 'error'}
                      />
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} flexWrap="wrap">
                        {r.distribution.map((d, di) => (
                          <Chip key={di} size="small"
                            label={`${d.ratingName}: ${d.count}`}
                            variant="outlined" />
                        ))}
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}
    </Box>
  );
}
