import { useTheme } from '@mui/material/styles';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Typography,
} from '@mui/material';
import type { SurveyHeatmapDto } from '../types';

interface SurveyHeatmapViewProps {
  data: SurveyHeatmapDto;
}

function interpolateColor(low: string, high: string, t: number): string {
  // Parse hex or rgba — theme colors are hex strings like "#xxxx"
  return t < 0.5
    ? `color-mix(in srgb, ${low} ${Math.round((1 - t * 2) * 100)}%, ${high})`
    : `color-mix(in srgb, ${high} ${Math.round((t * 2 - 1) * 100)}%, ${low})`;
}

export function SurveyHeatmapView({ data }: SurveyHeatmapViewProps) {
  const theme = useTheme();

  // Compute global min/max for normalisation
  let globalMin = Infinity;
  let globalMax = -Infinity;
  for (const row of data.matrix) {
    for (const val of row) {
      if (!isNaN(val)) {
        if (val < globalMin) globalMin = val;
        if (val > globalMax) globalMax = val;
      }
    }
  }
  const range = globalMax - globalMin || 1;

  function cellStyle(val: number): React.CSSProperties {
    if (isNaN(val)) return { backgroundColor: theme.palette.grey[200] };
    const t = (val - globalMin) / range;
    return {
      backgroundColor: interpolateColor(
        theme.palette.error.light,
        theme.palette.success.light,
        t,
      ),
    };
  }

  return (
    <TableContainer component={Paper} sx={{ overflowX: 'auto' }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell><strong>Department</strong></TableCell>
            {data.questionTexts.map((q, i) => (
              <TableCell key={i} align="center" sx={{ whiteSpace: 'nowrap', maxWidth: 120, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                <Typography variant="caption" title={q}>{q.length > 30 ? q.slice(0, 28) + '…' : q}</Typography>
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {data.departments.map((dept, deptIdx) => (
            <TableRow key={deptIdx}>
              <TableCell>{dept}</TableCell>
              {data.matrix[deptIdx]?.map((val, qIdx) => (
                <TableCell key={qIdx} align="center" style={cellStyle(val)}>
                  {isNaN(val) ? '–' : val.toFixed(1)}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
