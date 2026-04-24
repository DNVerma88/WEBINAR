import { Box, Card, CardContent, Divider, Typography } from '@mui/material';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
} from 'recharts';
import type { SurveyQuestionAnalyticsDto } from '../types';

interface QuestionStatsCardProps {
  question: SurveyQuestionAnalyticsDto;
}

export function QuestionStatsCard({ question }: QuestionStatsCardProps) {
  const { questionText, questionType, totalAnswers, optionStats, averageRating, minRating, maxRating, textAnswers } = question;

  return (
    <Card variant="outlined" sx={{ mb: 2 }}>
      <CardContent>
        <Typography variant="subtitle1" fontWeight="bold">{questionText}</Typography>
        <Typography variant="caption" color="text.secondary">
          {totalAnswers} response{totalAnswers !== 1 ? 's' : ''}
        </Typography>

        <Divider sx={{ my: 1 }} />

        {(questionType === 'SingleChoice' || questionType === 'MultipleChoice' || questionType === 'YesNo') && (
          <Box sx={{ height: 180 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={optionStats ?? []} layout="vertical" margin={{ left: 16 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis type="number" domain={[0, 100]} tickFormatter={v => `${v}%`} />
                <YAxis type="category" dataKey="optionValue" width={120} tick={{ fontSize: 12 }} />
                <Tooltip formatter={(v: unknown) => typeof v === 'number' ? [`${v.toFixed(1)}%`, 'Response Rate'] : [String(v), 'Response Rate']} />
                <Bar dataKey="percentage" fill="#1976d2" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </Box>
        )}

        {questionType === 'Rating' && (
          <Box>
            <Box sx={{ display: 'flex', gap: 3, mb: 1 }}>
              <Box>
                <Typography variant="h5" color="primary.main">
                  {averageRating != null ? averageRating.toFixed(2) : '—'}
                </Typography>
                <Typography variant="caption" color="text.secondary">Average</Typography>
              </Box>
              <Box>
                <Typography variant="h5">{minRating ?? '—'}</Typography>
                <Typography variant="caption" color="text.secondary">Min</Typography>
              </Box>
              <Box>
                <Typography variant="h5">{maxRating ?? '—'}</Typography>
                <Typography variant="caption" color="text.secondary">Max</Typography>
              </Box>
            </Box>
            {(optionStats ?? []).length > 0 && (
              <Box sx={{ height: 160 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={optionStats ?? []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="optionValue" />
                    <YAxis />
                    <Tooltip formatter={(v: unknown) => [String(v ?? ''), 'Count']} />
                    <Bar dataKey="count" fill="#1976d2" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </Box>
            )}
          </Box>
        )}

        {questionType === 'Text' && (
          <Box sx={{ maxHeight: 200, overflowY: 'auto' }}>
            {(textAnswers ?? []).length === 0 ? (
              <Typography variant="body2" color="text.secondary">No text answers yet.</Typography>
            ) : (
              (textAnswers ?? []).map((ans, i) => (
                <Box key={i} sx={{ py: 0.5, borderBottom: '1px solid', borderColor: 'divider' }}>
                  <Typography variant="body2">{ans}</Typography>
                </Box>
              ))
            )}
          </Box>
        )}
      </CardContent>
    </Card>
  );
}
