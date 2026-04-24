import { Alert, Box, Card, CardContent, Chip, Stack, Typography } from '@/components/ui';
import { Checkbox } from '@mui/material';
import type { ScreeningCandidate, Recommendation } from '../../types';

interface Props {
  candidate: ScreeningCandidate;
  onViewDetail?: (candidate: ScreeningCandidate) => void;
  selected?: boolean;
  onSelect?: (id: string, checked: boolean) => void;
}

/** Common empty-resume meta-commentary patterns emitted by the AI. */
function isEmptyResumeMeta(value: string): boolean {
  const lower = value.toLowerCase();
  return (
    lower.includes('was empty') ||
    lower.includes('is empty') ||
    lower.includes('completely blank') ||
    lower.includes('empty resume') ||
    lower.includes('resume is blank') ||
    lower.includes('no content') ||
    lower.includes('cannot assess') ||
    lower.includes('impossible to assess') ||
    lower.includes('provided resume is empty') ||
    lower.includes('no information') ||
    lower.includes('not provided') ||
    lower.includes('not available') ||
    lower.includes('not found') ||
    lower.includes('not stated') ||
    lower.includes('not present') ||
    lower.includes('not listed') ||
    lower.includes('not mentioned') ||
    lower.includes('not included')
  );
}

/** Suppresses AI meta-commentary that was stored before the prompt fix. */
function sanitizeSummary(value?: string | null): string | null {
  if (!value) return null;
  if (isEmptyResumeMeta(value)) return null;
  return value;
}

/** Removes empty-resume meta-commentary entries from a red-flags array. */
function sanitizeRedFlags(flags?: string[] | null): string[] {
  if (!flags) return [];
  return flags.filter((f) => !isEmptyResumeMeta(f));
}

function recommendationColor(rec?: Recommendation): 'success' | 'primary' | 'warning' | 'error' | 'default' {
  switch (rec) {
    case 'StrongFit': return 'success';
    case 'GoodFit': return 'primary';
    case 'MaybeFit': return 'warning';
    case 'NoFit': return 'error';
    default: return 'default';
  }
}

function ScoreCircle({ score }: { score: number }) {
  const color = score >= 75 ? '#2e7d32' : score >= 50 ? '#ed6c02' : '#d32f2f';
  return (
    <Box
      sx={{
        width: 64,
        height: 64,
        borderRadius: '50%',
        border: `4px solid ${color}`,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
      }}
    >
      <Typography variant="h6" fontWeight={700} sx={{ color }}>
        {Math.round(score)}
      </Typography>
    </Box>
  );
}

export function CandidateResultCard({ candidate, onViewDetail, selected, onSelect }: Props) {
  const cleanRedFlags = sanitizeRedFlags(candidate.redFlags);
  const hasRedFlags = cleanRedFlags.length > 0;
  const scoreSummary = sanitizeSummary(candidate.scoreSummary);

  // Failed candidates — show a minimal error card
  if (candidate.status === 'Failed') {
    return (
      <Card
        variant="outlined"
        sx={{ mb: 1.5, borderColor: selected ? 'primary.main' : 'error.light', borderWidth: selected ? 2 : 1 }}
      >
        <CardContent>
          <Box display="flex" alignItems="center" gap={1} flexWrap="wrap" mb={candidate.errorMessage ? 0.5 : 0}>
            {onSelect && (
              <Checkbox
                size="small"
                checked={selected ?? false}
                onChange={(e) => onSelect(candidate.id, e.target.checked)}
                onClick={(e) => e.stopPropagation()}
              />
            )}
            <Chip label="Failed" size="small" color="error" />
            <Typography variant="subtitle1" fontWeight={600} noWrap sx={{ color: 'text.secondary' }}>
              {candidate.candidateName ?? candidate.fileName}
            </Typography>
          </Box>
          {candidate.errorMessage && (
            <Alert severity="error" sx={{ mt: 0.5, py: 0.5 }}>
              {candidate.errorMessage}
            </Alert>
          )}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card
      variant="outlined"
      sx={{
        mb: 1.5,
        cursor: onViewDetail ? 'pointer' : 'default',
        borderColor: selected ? 'primary.main' : undefined,
        borderWidth: selected ? 2 : 1,
      }}
      onClick={() => onViewDetail?.(candidate)}
    >
      <CardContent>
        <Box display="flex" gap={2} alignItems="flex-start">
          {onSelect && (
            <Checkbox
              size="small"
              checked={selected ?? false}
              onChange={(e) => onSelect(candidate.id, e.target.checked)}
              onClick={(e) => e.stopPropagation()}
              sx={{ mt: -0.5 }}
            />
          )}
          {candidate.overallScore !== undefined && (
            <ScoreCircle score={candidate.overallScore} />
          )}

          <Box flex={1} minWidth={0}>
            <Box display="flex" alignItems="center" gap={1} flexWrap="wrap">
              <Typography variant="subtitle1" fontWeight={600} noWrap>
                {candidate.candidateName ?? candidate.fileName}
              </Typography>
              {candidate.recommendation && (
                <Chip
                  label={candidate.recommendation.replace(/([A-Z])/g, ' $1').trim()}
                  size="small"
                  color={recommendationColor(candidate.recommendation)}
                />
              )}
              {candidate.scoreSummary?.startsWith('[Stub mode]') && (
                <Chip label="Stub — No AI key" size="small" color="warning" variant="outlined" />
              )}
            </Box>

            {candidate.email && (
              <Typography variant="body2" color="text.secondary">
                {candidate.email}
              </Typography>
            )}

            {scoreSummary && (
              <Typography variant="body2" sx={{ mt: 0.5 }}>
                {scoreSummary}
              </Typography>
            )}

            {/* Matched skills */}
            {Array.isArray(candidate.skillsMatched) && candidate.skillsMatched.length > 0 && (
              <Stack direction="row" flexWrap="wrap" gap={0.5} mt={1}>
                {candidate.skillsMatched.map((s) => (
                  <Chip key={s} label={s} size="small" color="success" variant="outlined" />
                ))}
              </Stack>
            )}

            {/* Skill gaps */}
            {Array.isArray(candidate.skillsGap) && candidate.skillsGap.length > 0 && (
              <Stack direction="row" flexWrap="wrap" gap={0.5} mt={0.5}>
                {candidate.skillsGap.map((s) => (
                  <Chip key={s} label={s} size="small" color="warning" variant="outlined" />
                ))}
              </Stack>
            )}

            {hasRedFlags && (
              <Alert severity="warning" sx={{ mt: 1, py: 0 }}>
                {cleanRedFlags.join(' · ')}
              </Alert>
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}
