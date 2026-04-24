import { Box, Skeleton, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { surveysApi } from '../api/surveysApi';
import { QuestionStatsCard } from '../components/QuestionStatsCard';
import type { SurveyQuestionAnalyticsDto } from '../types';

interface ResultsTabProps {
  surveyId: string;
}

function toAnalyticsDto(q: {
  questionId: string;
  questionText: string;
  questionType: import('../types').SurveyQuestionType;
  totalAnswers: number;
  optionCounts: import('../types').OptionCountDto[] | null;
  averageRating?: number | null;
  minRating?: number | null;
  maxRating?: number | null;
  textAnswers: string[] | null;
}): SurveyQuestionAnalyticsDto {
  return {
    questionId: q.questionId,
    questionText: q.questionText,
    questionType: q.questionType,
    totalAnswers: q.totalAnswers,
    optionStats: (q.optionCounts ?? []).map(o => ({
      optionValue: o.optionValue,
      count: o.count,
      percentage: o.percentage,
    })),
    averageRating: q.averageRating,
    minRating: q.minRating,
    maxRating: q.maxRating,
    textAnswers: q.textAnswers ?? [],
  };
}

export function ResultsTab({ surveyId }: ResultsTabProps) {
  const { data, isLoading, error } = useQuery({
    queryKey: ['surveys', surveyId, 'results'],
    queryFn: () => surveysApi.getResults(surveyId),
  });

  if (isLoading) {
    return (
      <Box>
        {[1, 2, 3].map(i => (
          <Skeleton key={i} variant="rounded" height={180} sx={{ mb: 2 }} />
        ))}
      </Box>
    );
  }

  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Box sx={{ mb: 2 }}>
        <Typography variant="subtitle2" color="text.secondary">
          Response Rate: {data.responseRatePct != null ? data.responseRatePct.toFixed(1) : '0.0'}% &nbsp;|&nbsp;
          {data.totalResponded} / {data.totalInvited} responded
        </Typography>
      </Box>

      {data.questionResults.length === 0 ? (
        <Typography variant="body2" color="text.secondary">No responses yet.</Typography>
      ) : (
        data.questionResults.map(q => (
          <QuestionStatsCard key={q.questionId} question={toAnalyticsDto(q)} />
        ))
      )}
    </Box>
  );
}
