import { AddIcon, Box, Button, Card, CardContent, Chip, Grid, GroupIcon, TextField, Typography } from '@/components/ui';
import { memo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { communitiesApi } from '../../shared/api/communities';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole, type CommunityDto } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';

// FE-20: Per-card mutation state — each card manages its own join/leave mutation
// so only the affected card re-renders, not the entire list.
const CommunityCard = memo(function CommunityCard({ community }: { community: CommunityDto }) {
  const qc = useQueryClient();
  const toast = useToast();

  const joinMutation = useMutation({
    mutationFn: communitiesApi.joinCommunity,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['communities'] }); toast.success('Joined community!'); },
    onError: () => toast.error('Failed to join community.'),
  });

  const leaveMutation = useMutation({
    mutationFn: communitiesApi.leaveCommunity,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['communities'] }); toast.success('Left community.'); },
    onError: () => toast.error('Failed to leave community.'),
  });

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardContent sx={{ flexGrow: 1 }}>
        <Box display="flex" alignItems="center" gap={1} mb={1}>
          <GroupIcon color="primary" />
          <Typography
            variant="h6"
            component={Link}
            to={`/communities/${community.id}`}
            sx={{ textDecoration: 'none', color: 'inherit' }}
          >
            {community.name}
          </Typography>
        </Box>
        {community.description && (
          <Typography variant="body2" color="text.secondary" mb={1}>
            {community.description}
          </Typography>
        )}
        <Chip label={`${community.memberCount} members`} size="small" variant="outlined" />
      </CardContent>
      <Box px={2} pb={2}>
        <ApiErrorAlert error={joinMutation.error ?? leaveMutation.error} />
        {community.isMember ? (
          <Button
            size="small"
            variant="outlined"
            color="inherit"
            onClick={() => leaveMutation.mutate(community.id)}
            disabled={leaveMutation.isPending}
          >
            Leave
          </Button>
        ) : (
          <Button
            size="small"
            variant="contained"
            onClick={() => joinMutation.mutate(community.id)}
            disabled={joinMutation.isPending}
          >
            Join
          </Button>
        )}
      </Box>
    </Card>
  );
});

export default function CommunityListPage() {
  usePageTitle('Communities');
  const { hasRole } = useAuth();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useQuery({
    queryKey: ['communities', search, page],
    queryFn: ({ signal }) => communitiesApi.getCommunities({ search: search || undefined, page, pageSize: 12 }, signal),
  });

  const canCreate = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin) || hasRole(UserRole.KnowledgeTeam);

  return (
    <Box>
      <PageHeader
        title="Communities"
        subtitle="Join knowledge communities and stay connected"
        actions={
          canCreate ? (
            <Button variant="contained" startIcon={<AddIcon />} component={Link} to="/communities/new">
              New Community
            </Button>
          ) : undefined
        }
      />

      <Box display="flex" gap={2} mb={3}>
        <TextField
          size="small"
          label="Search"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 280 }}
        />
      </Box>

      <ApiErrorAlert error={error} />

      {isLoading ? (
        <LoadingOverlay />
      ) : (
        <>
          <Grid container spacing={3}>
            {data?.data.map((community) => (
              <Grid size={{ xs: 12, sm: 6, md: 4 }} key={community.id}>
                <CommunityCard community={community} />
              </Grid>
            ))}
          </Grid>

          {data && data.data.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={6}>
              No communities found.
            </Typography>
          )}

          {data && data.totalPages > 1 && (
            <Box display="flex" justifyContent="center" mt={4} gap={1}>
              <Button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <Chip label={`${page} / ${data.totalPages}`} />
              <Button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </Box>
          )}
        </>
      )}
    </Box>
  );
}

