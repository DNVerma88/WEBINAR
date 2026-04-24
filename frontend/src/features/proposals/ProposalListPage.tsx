import { AddIcon, Box, Button, Chip, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@/components/ui';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { proposalsApi } from '../../shared/api/proposals';
import { categoriesApi } from '../../shared/api/categories';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ProposalStatus, PROPOSAL_STATUS_LABELS } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import { ProposalCard } from './ProposalCard';

export default function ProposalListPage() {
  usePageTitle('Proposals');
  const { hasRole } = useAuth();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<ProposalStatus | ''>('');
  const [categoryId, setCategoryId] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useQuery({
    queryKey: ['proposals', search, status, categoryId, page],
    queryFn: ({ signal }) =>
      proposalsApi.getProposals({
        search: search || undefined,
        status: status !== '' ? status : undefined,
        categoryId: categoryId || undefined,
        page,
        pageSize: 12,
      }, signal),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => categoriesApi.getCategories(signal),
  });

  const canSubmitProposal = hasRole(UserRole.Contributor) || hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);

  return (
    <Box>
      <PageHeader
        title="Session Proposals"
        subtitle="Submit and track knowledge session proposals"
        actions={
          canSubmitProposal ? (
            <Button variant="contained" startIcon={<AddIcon />} component={Link} to="/proposals/new">
              New Proposal
            </Button>
          ) : undefined
        }
      />

      {/* Filters */}
      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 240 }}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => { setStatus(e.target.value as ProposalStatus | ''); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {Object.values(ProposalStatus).map((v) => (
              <MenuItem key={v} value={v}>{PROPOSAL_STATUS_LABELS[v]}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Category</InputLabel>
          <Select value={categoryId} label="Category" onChange={(e) => { setCategoryId(e.target.value); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {categories?.map((c) => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
          </Select>
        </FormControl>
      </Box>

      <ApiErrorAlert error={error} />

      {isLoading ? (
        <LoadingOverlay />
      ) : (
        <>
          <Box display="grid" gridTemplateColumns="repeat(auto-fill, minmax(320px, 1fr))" gap={3}>
            {data?.data.map((proposal) => (
              <ProposalCard key={proposal.id} proposal={proposal} />
            ))}
          </Box>

          {data && data.data.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={6}>
              No proposals found.
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
