import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { learningPathsApi } from '../../shared/api/learningPaths';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { DifficultyLevelLabel, UserRole, DifficultyLevel, DIFFICULTY_LEVEL_LABELS } from '../../shared/types';

const difficultyColor: Record<string, 'success' | 'warning' | 'error'> = {
  Beginner: 'success',
  Intermediate: 'warning',
  Advanced: 'error',
};

export default function LearningPathListPage() {
  usePageTitle('Learning Paths');
  const navigate = useNavigate();
  const { hasRole } = useAuth();

  const [search, setSearch] = useState('');
  const [difficulty, setDifficulty] = useState<DifficultyLevel | ''>('');
  const [page, setPage] = useState(1);

  const canManage = hasRole(UserRole.KnowledgeTeam) || hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);

  const { data, isLoading, error } = useQuery({
    queryKey: ['learning-paths', search, difficulty, page],
    queryFn: ({ signal }) =>
      learningPathsApi.getPaths({
        searchTerm: search || undefined,
        difficultyLevel: difficulty !== '' ? difficulty : undefined,
        pageNumber: page,
        pageSize: 12,
      }, signal),
  });

  return (
    <Box>
      <PageHeader
        title="Learning Paths"
        subtitle="Structured learning journeys to build your skills"
        actions={
          canManage ? (
            <Button variant="contained" onClick={() => navigate('/learning-paths/new')}>
              Create Learning Path
            </Button>
          ) : undefined
        }
      />

      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search paths"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 280 }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Difficulty</InputLabel>
          <Select
            value={difficulty}
            label="Difficulty"
            onChange={(e) => { setDifficulty(e.target.value as DifficultyLevel | ''); setPage(1); }}
          >
            <MenuItem value="">All</MenuItem>
            {Object.values(DifficultyLevel).map((v) => (
              <MenuItem key={v} value={v}>{DIFFICULTY_LEVEL_LABELS[v]}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      <Box display="grid" gridTemplateColumns="repeat(auto-fill, minmax(320px, 1fr))" gap={3}>
        {data?.data.map((path) => (
          <Card
            key={path.id}
            sx={{ cursor: 'pointer', '&:hover': { boxShadow: 6 } }}
            onClick={() => navigate(`/learning-paths/${path.id}`)}
          >
            <CardContent>
              <Stack spacing={1}>
                <Box display="flex" justifyContent="space-between" alignItems="flex-start">
                  <Typography variant="h6" fontWeight={700} sx={{ fontSize: '1rem' }}>
                    {path.title}
                  </Typography>
                  <Chip
                    label={DifficultyLevelLabel[path.difficultyLevel]}
                    color={difficultyColor[path.difficultyLevel] ?? 'default'}
                    size="small"
                  />
                </Box>
                {path.description && (
                  <Typography variant="body2" color="text.secondary" sx={{ WebkitLineClamp: 2, overflow: 'hidden', display: '-webkit-box', WebkitBoxOrient: 'vertical' }}>
                    {path.description}
                  </Typography>
                )}
                <Box display="flex" gap={1} flexWrap="wrap">
                  {path.categoryName && <Chip label={path.categoryName} size="small" variant="outlined" />}
                  <Chip label={`${path.itemCount} items`} size="small" variant="outlined" />
                  {path.estimatedDurationMinutes > 0 && (
                    <Chip label={`${path.estimatedDurationMinutes} min`} size="small" variant="outlined" />
                  )}
                </Box>
                {canManage && !path.isPublished && (
                  <Chip label="Draft" color="warning" size="small" />
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Box>

      {data && data.totalCount > 12 && (
        <Box display="flex" justifyContent="center" gap={2} mt={4}>
          <Button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
          <Typography alignSelf="center">Page {page}</Typography>
          <Button disabled={data.data.length < 12} onClick={() => setPage((p) => p + 1)}>Next</Button>
        </Box>
      )}
    </Box>
  );
}
