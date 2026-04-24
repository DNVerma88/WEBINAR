import { Box, Typography } from '@mui/material';
import {
  RadialBarChart,
  RadialBar,
  PolarAngleAxis,
  ResponsiveContainer,
} from 'recharts';

interface NpsGaugeProps {
  score: number; // -100 to +100
}

function scoreColor(score: number): string {
  if (score >= 50) return '#2e7d32';  // green
  if (score >= 0)  return '#ed6c02';  // amber
  return '#d32f2f';                   // red
}

export function NpsGauge({ score }: NpsGaugeProps) {
  // Map -100…+100 to 0…100 for RadialBar fill percentage
  const normalised = (score + 100) / 2;
  const color = scoreColor(score);

  const data = [{ value: normalised, fill: color }];

  return (
    <Box sx={{ position: 'relative', width: 160, height: 100 }}>
      <ResponsiveContainer width="100%" height="100%">
        <RadialBarChart
          cx="50%"
          cy="100%"
          innerRadius="60%"
          outerRadius="100%"
          startAngle={180}
          endAngle={0}
          data={data}
          barSize={16}
        >
          <PolarAngleAxis type="number" domain={[0, 100]} angleAxisId={0} tick={false} />
          <RadialBar
            dataKey="value"
            cornerRadius={4}
            background={{ fill: '#eee' }}
          />
        </RadialBarChart>
      </ResponsiveContainer>
      <Box
        sx={{
          position: 'absolute',
          bottom: 4,
          left: 0,
          right: 0,
          textAlign: 'center',
        }}
      >
        <Typography variant="h5" fontWeight="bold" sx={{ color }}>
          {score > 0 ? `+${score}` : score}
        </Typography>
        <Typography variant="caption" color="text.secondary">NPS</Typography>
      </Box>
    </Box>
  );
}
