import { Box, Card, CardContent, CommunityIcon, Grid, ProposalIcon, SessionIcon, Typography } from '@/components/ui';
import { Link } from 'react-router-dom';
import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { sessionsApi } from '../../shared/api/sessions';
import { proposalsApi } from '../../shared/api/proposals';
import { communitiesApi } from '../../shared/api/communities';
import { SessionStatus } from '../../shared/types';

export default function DashboardPage() {
  usePageTitle('Dashboard');
  const { user } = useAuth();

  const { data: upcomingSessions } = useQuery({
    queryKey: ['sessions', 'dashboard-upcoming'],
    queryFn: ({ signal }) => sessionsApi.getSessions({ status: SessionStatus.Scheduled, pageSize: 1 }, signal),
  });

  const { data: myProposals } = useQuery({
    queryKey: ['proposals', 'dashboard-mine'],
    queryFn: ({ signal }) => proposalsApi.getProposals({ pageSize: 1 }, signal),
  });

  const { data: myCommunities } = useQuery({
    queryKey: ['communities', 'dashboard-mine'],
    queryFn: ({ signal }) => communitiesApi.getCommunities({ pageSize: 1 }, signal),
  });

  // FE-10: memoize stats and quickLinks so JSX element objects aren't recreated on
  // every render, preventing unnecessary re-renders in any child that depends on them
  const stats = useMemo(() => [
    {
      label: 'Upcoming Sessions',
      value: upcomingSessions?.totalCount ?? '—',
      to: '/sessions',
      icon: <SessionIcon fontSize="large" color="primary" />,
      color: 'primary.main',
    },
    {
      label: 'My Proposals',
      value: myProposals?.totalCount ?? '—',
      to: '/proposals',
      icon: <ProposalIcon fontSize="large" color="secondary" />,
      color: 'secondary.main',
    },
    {
      label: 'Communities',
      value: myCommunities?.totalCount ?? '—',
      to: '/communities',
      icon: <CommunityIcon fontSize="large" color="success" />,
      color: 'success.main',
    },
  ], [upcomingSessions?.totalCount, myProposals?.totalCount, myCommunities?.totalCount]);

  const quickLinks = useMemo(() => [
    { label: 'Browse Sessions', to: '/sessions', icon: <SessionIcon fontSize="large" color="primary" /> },
    { label: 'Submit a Proposal', to: '/proposals/new', icon: <ProposalIcon fontSize="large" color="secondary" /> },
    { label: 'Explore Communities', to: '/communities', icon: <CommunityIcon fontSize="large" color="success" /> },
  ], []);

  return (
    <Box>
      <Typography variant="h5" mb={1}>
        Welcome back, {user?.fullName ?? 'there'} 👋
      </Typography>
      <Typography variant="body2" color="text.secondary" mb={4}>
        Here's what's happening on KnowHub today.
      </Typography>

      <Typography variant="h6" mb={2}>Overview</Typography>
      <Grid container spacing={3} mb={4}>
        {stats.map((s) => (
          <Grid size={{ xs: 12, sm: 4 }} key={s.to}>
            <Card component={Link} to={s.to} sx={{ textDecoration: 'none', '&:hover': { boxShadow: 4 }, transition: 'box-shadow 0.2s' }}>
              <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 3 }}>
                {s.icon}
                <Box>
                  <Typography variant="h4" fontWeight="bold" color={s.color}>
                    {s.value}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">{s.label}</Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Typography variant="h6" mb={2}>Quick Actions</Typography>
      <Grid container spacing={3}>
        {quickLinks.map((card) => (
          <Grid size={{ xs: 12, sm: 6, md: 4 }} key={card.to}>
            <Card
              component={Link}
              to={card.to}
              sx={{ textDecoration: 'none', '&:hover': { boxShadow: 4 }, transition: 'box-shadow 0.2s' }}
            >
              <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 3 }}>
                {card.icon}
                <Typography variant="h6">{card.label}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
