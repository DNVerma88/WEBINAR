import { AddIcon, Box, Button, Chip, FormControl, Grid, InputLabel, MenuItem, Select, Skeleton, Stack, TextField, Typography } from '@/components/ui';
import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { sessionsApi } from '../../shared/api/sessions';
import { categoriesApi } from '../../shared/api/categories';
import { tagsApi } from '../../shared/api/tags';
import { PageHeader } from '../../shared/components/PageHeader';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { SessionFormat, SessionStatus, SESSION_FORMAT_LABELS, SESSION_STATUS_LABELS } from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import { SessionCard } from './SessionCard';

export default function SessionListPage() {
  usePageTitle('Sessions');
  const { hasRole } = useAuth();
  // FE-02: decouple raw input state from the query key so searches are debounced
  // and only trigger an API call after the user stops typing (350 ms)
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [format, setFormat] = useState<SessionFormat | ''>('');
  const [status, setStatus] = useState<SessionStatus | ''>('');
  const [tagId, setTagId] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput), 350);
    return () => clearTimeout(timer);
  }, [searchInput]);
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useQuery({
    queryKey: ['sessions', search, categoryId, format, status, tagId, page],
    queryFn: ({ signal }) =>
      sessionsApi.getSessions({
        searchTerm: search || undefined,
        categoryId: categoryId || undefined,
        format: format !== '' ? format : undefined,
        status: status !== '' ? status : undefined,
        tagId: tagId || undefined,
        pageNumber: page,
        pageSize: 12,
      }, signal),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => categoriesApi.getCategories(signal),
    staleTime: 5 * 60_000,
  });

  const { data: tagsData } = useQuery({
    queryKey: ['tags'],
    queryFn: ({ signal }) => tagsApi.getTags({ pageSize: 200 }, signal),
    staleTime: 5 * 60_000,
  });

  const canCreateSession = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin) || hasRole(UserRole.KnowledgeTeam);

  return (
    <Box>
      <PageHeader
        title="Sessions"
        subtitle="Discover and register for knowledge sessions"
        actions={
          canCreateSession ? (
            <Button variant="contained" startIcon={<AddIcon />} component={Link} to="/sessions/new">
              Schedule Session
            </Button>
          ) : undefined
        }
      />

      {/* Filters */}
      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search"
          value={searchInput}
          onChange={(e) => { setSearchInput(e.target.value); setPage(1); }}
          sx={{ minWidth: 240 }}
        />
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Category</InputLabel>
          <Select value={categoryId} label="Category" onChange={(e) => { setCategoryId(e.target.value); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {categories?.map((c) => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Format</InputLabel>
          <Select value={format} label="Format" onChange={(e) => { setFormat(e.target.value as SessionFormat | ''); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {Object.values(SessionFormat).map((v) => (
              <MenuItem key={v} value={v}>{SESSION_FORMAT_LABELS[v]}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => { setStatus(e.target.value as SessionStatus | ''); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {Object.values(SessionStatus).map((v) => (
              <MenuItem key={v} value={v}>{SESSION_STATUS_LABELS[v]}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Tag</InputLabel>
          <Select value={tagId} label="Tag" onChange={(e) => { setTagId(e.target.value); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            {tagsData?.data.map((t) => <MenuItem key={t.id} value={t.id}>{t.name}</MenuItem>)}
          </Select>
        </FormControl>
      </Box>

      <ApiErrorAlert error={error} />

      {isLoading ? (
        <Grid container spacing={3}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Grid size={{ xs: 12, sm: 6, md: 4 }} key={i}>
              <Stack spacing={1}>
                <Skeleton variant="rectangular" height={140} sx={{ borderRadius: 1 }} />
                <Skeleton width="70%" />
                <Skeleton width="50%" />
              </Stack>
            </Grid>
          ))}
        </Grid>
      ) : (
        <>
          <Grid container spacing={3}>
            {data?.data.map((session) => (
              <Grid size={{ xs: 12, sm: 6, md: 4 }} key={session.id}>
                <SessionCard session={session} />
              </Grid>
            ))}
          </Grid>

          {data && data.data.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={6}>
              No sessions found.
            </Typography>
          )}

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <Box display="flex" justifyContent="center" mt={4} gap={1}>
              <Button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <Chip label={`${page} / ${data.totalPages}`} />
              <Button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
                Next
              </Button>
            </Box>
          )}
        </>
      )}
    </Box>
  );
}
