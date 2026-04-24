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
import { Link, useNavigate } from 'react-router-dom';
import { knowledgeAssetsApi } from '../../shared/api/knowledgeAssets';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { KnowledgeAssetType, KnowledgeAssetTypeLabel } from '../../shared/types';

export default function KnowledgeAssetListPage() {
  usePageTitle('Knowledge Assets');
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [assetType, setAssetType] = useState<KnowledgeAssetType | ''>('');
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useQuery({
    queryKey: ['knowledge-assets', search, assetType, page],
    queryFn: ({ signal }) =>
      knowledgeAssetsApi.getAssets({
        searchTerm: search || undefined,
        assetType: assetType !== '' ? assetType : undefined,
        pageNumber: page,
        pageSize: 15,
      }, signal),
  });

  return (
    <Box>
      <PageHeader
        title="Knowledge Assets"
        subtitle="Browse recordings, slides, docs, and more"
        actions={
          <Button variant="contained" onClick={() => navigate('/knowledge-assets/new')}>
            Upload Asset
          </Button>
        }
      />

      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search assets"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 280 }}
        />
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Type</InputLabel>
          <Select
            value={assetType}
            label="Type"
            onChange={(e) => { setAssetType(e.target.value as KnowledgeAssetType | ''); setPage(1); }}
          >
            <MenuItem value="">All</MenuItem>
            {Object.values(KnowledgeAssetType).map((v) => (
              <MenuItem key={v} value={v}>{KnowledgeAssetTypeLabel[v] ?? v}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      <Stack spacing={2}>
        {data?.data.map((asset) => (
          <Card key={asset.id}>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="flex-start" flexWrap="wrap" gap={1}>
                <Box flexGrow={1}>
                  <Typography
                    variant="h6"
                    component={Link}
                    to={`/knowledge-assets/${asset.id}`}
                    sx={{ fontSize: '1rem', textDecoration: 'none', color: 'primary.main', '&:hover': { textDecoration: 'underline' } }}
                  >
                    {asset.title}
                  </Typography>
                  {asset.description && (
                    <Typography variant="body2" color="text.secondary" mt={0.5}>
                      {asset.description}
                    </Typography>
                  )}
                </Box>
                <Box display="flex" gap={1} flexWrap="wrap">
                  <Chip label={KnowledgeAssetTypeLabel[asset.assetType] ?? 'Unknown'} size="small" color="primary" variant="outlined" />
                  {asset.isVerified && <Chip label="Verified" size="small" color="success" />}
                  {!asset.isPublic && <Chip label="Private" size="small" color="warning" />}
                </Box>
              </Box>
              <Box display="flex" gap={2} mt={1}>
                <Typography variant="caption" color="text.secondary">
                  {asset.viewCount} views · {asset.downloadCount} downloads
                </Typography>
              </Box>
            </CardContent>
          </Card>
        ))}
        {data?.data.length === 0 && (
          <Typography color="text.secondary" textAlign="center" py={4}>
            No assets found.
          </Typography>
        )}
      </Stack>
    </Box>
  );
}
