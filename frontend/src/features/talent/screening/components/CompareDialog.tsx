import {
  Alert,
  Box,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@/components/ui';
import { CloseIcon } from '@/components/ui';
import { IconButton } from '@mui/material';
import type { ScreeningCandidate, Recommendation } from '../../types';

/** Same empty-resume meta-commentary filter used in CandidateResultCard. */
function isEmptyResumeMeta(value: string): boolean {
  const lower = value.toLowerCase();
  return (
    lower.includes('was empty') || lower.includes('is empty') ||
    lower.includes('completely blank') || lower.includes('empty resume') ||
    lower.includes('resume is blank') || lower.includes('no content') ||
    lower.includes('cannot assess') || lower.includes('impossible to assess') ||
    lower.includes('provided resume is empty') || lower.includes('no information') ||
    lower.includes('not provided') || lower.includes('not available') ||
    lower.includes('not stated') || lower.includes('not present') ||
    lower.includes('not listed') || lower.includes('not mentioned') ||
    lower.includes('not included') || lower.includes('not found')
  );
}

interface Props {
  open: boolean;
  onClose: () => void;
  candidateA: ScreeningCandidate;
  candidateB: ScreeningCandidate;
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

function ScoreCircle({ score, small }: { score: number; small?: boolean }) {
  const color = score >= 75 ? '#2e7d32' : score >= 50 ? '#ed6c02' : '#d32f2f';
  const size = small ? 52 : 64;
  return (
    <Box
      sx={{
        width: size,
        height: size,
        borderRadius: '50%',
        border: `4px solid ${color}`,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
      }}
    >
      <Typography variant={small ? 'body1' : 'h6'} fontWeight={700} sx={{ color }}>
        {Math.round(score)}
      </Typography>
    </Box>
  );
}

function SubScore({ label, value }: { label: string; value?: number | null }) {
  if (value === undefined || value === null) return null;
  const display = label === 'Similarity' ? `${(value * 100).toFixed(0)}%` : `${Math.round(value)}`;
  const color = value >= (label === 'Similarity' ? 0.75 : 75)
    ? 'success.main'
    : value >= (label === 'Similarity' ? 0.5 : 50)
    ? 'warning.main'
    : 'error.main';
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={600} color={color}>{display}</Typography>
    </Box>
  );
}

interface CandidateColumnProps {
  candidate: ScreeningCandidate;
  label: 'A' | 'B';
}

function CandidateColumn({ candidate, label }: CandidateColumnProps) {
  const scoreSummary = candidate.scoreSummary && !isEmptyResumeMeta(candidate.scoreSummary)
    ? candidate.scoreSummary
    : null;
  const cleanRedFlags = (candidate.redFlags ?? []).filter((f) => !isEmptyResumeMeta(f));
  return (
    <Box flex={1} minWidth={0}>
      <Box display="flex" alignItems="center" gap={1} mb={1.5}>
        <Chip label={`Candidate ${label}`} size="small" color="primary" variant="outlined" />
        <Typography variant="subtitle1" fontWeight={700} noWrap>
          {candidate.candidateName ?? candidate.fileName}
        </Typography>
      </Box>

      {/* Overall score + recommendation */}
      <Box display="flex" alignItems="center" gap={2} mb={1.5}>
        {candidate.overallScore !== undefined && (
          <ScoreCircle score={candidate.overallScore} />
        )}
        <Box>
          {candidate.recommendation && (
            <Chip
              label={candidate.recommendation.replace(/([A-Z])/g, ' $1').trim()}
              size="small"
              color={recommendationColor(candidate.recommendation)}
              sx={{ mb: 0.5 }}
            />
          )}
          <Stack direction="row" spacing={2} mt={0.5}>
            <SubScore label="Similarity" value={candidate.semanticSimilarityScore} />
            <SubScore label="Skills" value={candidate.skillsDepthScore} />
            <SubScore label="Legitimacy" value={candidate.legitimacyScore} />
          </Stack>
        </Box>
      </Box>

      {/* Contact */}
      {(candidate.email || candidate.phone) && (
        <Box mb={1}>
          {candidate.email && (
            <Typography variant="body2" color="text.secondary" noWrap>{candidate.email}</Typography>
          )}
          {candidate.phone && (
            <Typography variant="body2" color="text.secondary">{candidate.phone}</Typography>
          )}
        </Box>
      )}

      {/* Summary */}
      {scoreSummary && (
        <Typography variant="body2" sx={{ mb: 1 }}>{scoreSummary}</Typography>
      )}

      {/* Matched skills */}
      {Array.isArray(candidate.skillsMatched) && candidate.skillsMatched.length > 0 && (
        <Box mb={1}>
          <Typography variant="caption" color="text.secondary" fontWeight={600} display="block" mb={0.5}>
            Matched Skills
          </Typography>
          <Stack direction="row" flexWrap="wrap" gap={0.5}>
            {candidate.skillsMatched.map((s) => (
              <Chip key={s} label={s} size="small" color="success" variant="outlined" />
            ))}
          </Stack>
        </Box>
      )}

      {/* Skill gaps */}
      {Array.isArray(candidate.skillsGap) && candidate.skillsGap.length > 0 && (
        <Box mb={1}>
          <Typography variant="caption" color="text.secondary" fontWeight={600} display="block" mb={0.5}>
            Skill Gaps
          </Typography>
          <Stack direction="row" flexWrap="wrap" gap={0.5}>
            {candidate.skillsGap.map((s) => (
              <Chip key={s} label={s} size="small" color="warning" variant="outlined" />
            ))}
          </Stack>
        </Box>
      )}

      {/* Red flags */}
      {cleanRedFlags.length > 0 && (
        <Alert severity="warning" sx={{ py: 0.5 }}>
          {cleanRedFlags.join(' · ')}
        </Alert>
      )}
    </Box>
  );
}

export function CompareDialog({ open, onClose, candidateA, candidateB }: Props) {
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">Candidate Comparison</Typography>
          <IconButton size="small" onClick={onClose}>
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={3}
          divider={<Divider orientation="vertical" flexItem />}
          alignItems="flex-start"
        >
          <CandidateColumn candidate={candidateA} label="A" />
          <CandidateColumn candidate={candidateB} label="B" />
        </Stack>
      </DialogContent>
    </Dialog>
  );
}
