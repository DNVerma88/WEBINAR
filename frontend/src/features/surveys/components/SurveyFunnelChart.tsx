import { Box, Typography } from '@mui/material';
import {
  FunnelChart,
  Funnel,
  LabelList,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import type { SurveyParticipationFunnelDto } from '../types';

interface SurveyFunnelChartProps {
  data: SurveyParticipationFunnelDto;
}

export function SurveyFunnelChart({ data }: SurveyFunnelChartProps) {
  const totalInvited = data.totalInvited || 1;

  const stages = [
    { name: 'Invited',   value: data.totalInvited,       pct: 100 },
    { name: 'Sent',      value: data.totalEmailsSent,     pct: Math.round(data.totalEmailsSent / totalInvited * 100) },
    { name: 'Opened',    value: data.totalTokensAccessed, pct: Math.round(data.totalTokensAccessed / totalInvited * 100) },
    { name: 'Submitted', value: data.totalSubmitted,      pct: Math.round(data.totalSubmitted / totalInvited * 100) },
  ];

  return (
    <Box>
      <ResponsiveContainer width="100%" height={220}>
        <FunnelChart>
          <Tooltip
            formatter={(value: unknown, _name: unknown, props: { payload?: { pct?: number } }) =>
              [`${String(value)} (${props?.payload?.pct ?? 0}%)`, '']
            }
          />
          <Funnel
            dataKey="value"
            data={stages}
            isAnimationActive
          >
            <LabelList
              position="insideTop"
              content={({ value, x, y, width, height, index }) => {
                if (value === undefined || x === undefined || y === undefined || width === undefined || height === undefined) return null;
                const stage = stages[index as number];
                return (
                  <text
                    x={Number(x) + Number(width) / 2}
                    y={Number(y) + Number(height) / 2}
                    textAnchor="middle"
                    dominantBaseline="middle"
                    fill="#fff"
                    fontSize={13}
                    fontWeight="bold"
                  >
                    {stage.name}: {stage.value} ({stage.pct}%)
                  </text>
                );
              }}
            />
          </Funnel>
        </FunnelChart>
      </ResponsiveContainer>

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mt: 1 }}>
        {stages.map(s => (
          <Box key={s.name} sx={{ textAlign: 'center', minWidth: 80 }}>
            <Typography variant="h6">{s.value}</Typography>
            <Typography variant="caption" color="text.secondary">{s.name}</Typography>
          </Box>
        ))}
      </Box>
    </Box>
  );
}
