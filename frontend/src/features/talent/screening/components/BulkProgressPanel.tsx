import { Alert, Box, Chip, LinearProgress, Stack, Typography } from '@/components/ui';
import { CheckCircleIcon } from '@/components/ui';
import { useState, useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useScreeningProgress } from '../../hooks/useScreeningProgress';
import type { ScreeningProgressEvent } from '../../types';

interface RecentCandidate {
  candidateId: string;
  fileName: string;
  overallScore?: number;
  recommendation?: string;
}

interface Props {
  jobId: string;
  totalCandidates: number;
}

function recommendationColor(rec?: string): 'success' | 'primary' | 'warning' | 'error' | 'default' {
  switch (rec) {
    case 'StrongFit': return 'success';
    case 'GoodFit': return 'primary';
    case 'MaybeFit': return 'warning';
    case 'NoFit': return 'error';
    default: return 'default';
  }
}

export function BulkProgressPanel({ jobId, totalCandidates }: Props) {
  const queryClient = useQueryClient();
  const [progress, setProgress] = useState({ processed: 0, percent: 0 });
  const [recentCandidates, setRecentCandidates] = useState<RecentCandidate[]>([]);
  const [isCompleted, setIsCompleted] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleProgress = useCallback((event: ScreeningProgressEvent) => {
    setProgress({ processed: event.processed, percent: event.percentComplete });
    if (event.latestCandidate) {
      setRecentCandidates((prev) => [event.latestCandidate!, ...prev].slice(0, 10));
    }
  }, []);

  const handleCompleted = useCallback(() => {
    setIsCompleted(true);
    void queryClient.invalidateQueries({ queryKey: ['screening', jobId] });
  }, [queryClient, jobId]);

  const handleFailed = useCallback((_jobId: string, error: string) => {
    setErrorMessage(error || 'Screening job failed. Please retry.');
    void queryClient.invalidateQueries({ queryKey: ['screening', jobId] });
  }, [queryClient, jobId]);

  useScreeningProgress(jobId, handleProgress, handleCompleted, handleFailed);

  return (
    <Box>
      <Box display="flex" alignItems="center" gap={2} mb={1}>
        <Typography variant="body2" fontWeight={600}>
          Screening in progress…
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {progress.processed} / {totalCandidates} candidates
        </Typography>
      </Box>

      <LinearProgress
        variant="determinate"
        value={progress.percent}
        sx={{ height: 8, borderRadius: 4, mb: 2 }}
      />

      {isCompleted && (
        <Box display="flex" alignItems="center" gap={1} mb={2} color="success.main">
          <CheckCircleIcon fontSize="small" />
          <Typography variant="body2">Screening complete!</Typography>
        </Box>
      )}

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      )}

      {recentCandidates.length > 0 && (
        <Box>
          <Typography variant="caption" color="text.secondary" display="block" mb={1}>
            Recently scored:
          </Typography>
          <Stack spacing={1}>
            {recentCandidates.map((c) => (
              <Box key={c.candidateId} display="flex" alignItems="center" gap={1}>
                <Typography variant="body2" sx={{ flex: 1 }} noWrap>
                  {c.fileName}
                </Typography>
                {c.overallScore !== undefined && (
                  <Chip
                    label={`${Math.round(c.overallScore)}%`}
                    size="small"
                    variant="outlined"
                  />
                )}
                {c.recommendation && (
                  <Chip
                    label={c.recommendation}
                    size="small"
                    color={recommendationColor(c.recommendation)}
                  />
                )}
              </Box>
            ))}
          </Stack>
        </Box>
      )}
    </Box>
  );
}
