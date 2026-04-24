import { Card, CardActionArea, CardContent, Chip, Stack, Typography } from '@/components/ui';
import { Link } from 'react-router-dom';
import type { SessionProposalDto } from '../../shared/types';
import { ProposalStatus, DifficultyLevel, PROPOSAL_STATUS_LABELS, DIFFICULTY_LEVEL_LABELS } from '../../shared/types';

const statusColor: Record<
  ProposalStatus,
  'default' | 'warning' | 'info' | 'success' | 'error' | 'primary'
> = {
  [ProposalStatus.Draft]: 'default',
  [ProposalStatus.Submitted]: 'info',
  [ProposalStatus.ManagerReview]: 'warning',
  [ProposalStatus.KnowledgeTeamReview]: 'primary',
  [ProposalStatus.Published]: 'success',
  [ProposalStatus.Scheduled]: 'info',
  [ProposalStatus.InProgress]: 'info',
  [ProposalStatus.Completed]: 'success',
  [ProposalStatus.Cancelled]: 'default',
  [ProposalStatus.Rejected]: 'error',
  [ProposalStatus.RevisionRequested]: 'warning',
};

interface ProposalCardProps {
  proposal: SessionProposalDto;
}

export function ProposalCard({ proposal }: ProposalCardProps) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea component={Link} to={`/proposals/${proposal.id}`} sx={{ height: '100%' }}>
        <CardContent>
          <Stack direction="row" spacing={1} mb={1} flexWrap="wrap">
            <Chip
              label={PROPOSAL_STATUS_LABELS[proposal.status as ProposalStatus] ?? proposal.status}
              color={statusColor[proposal.status]}
              size="small"
            />
            <Chip label={DIFFICULTY_LEVEL_LABELS[proposal.difficultyLevel as DifficultyLevel] ?? proposal.difficultyLevel} size="small" variant="outlined" />
          </Stack>
          <Typography variant="h6" gutterBottom sx={{ lineHeight: 1.3 }}>
            {proposal.title}
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            {proposal.proposerName} · {proposal.categoryName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {proposal.topic} · {proposal.estimatedDurationMinutes} min
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
