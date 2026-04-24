import {
  Box,
  Chip,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@/components/ui';
import { TablePagination } from '@mui/material';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { assessmentAuditApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';

type ChipColor = 'default' | 'primary' | 'error' | 'info' | 'success' | 'warning';

const ACTION_COLORS: Record<string, ChipColor> = {
  Submitted:       'success',
  PeriodPublished: 'primary',
  Reopened:        'warning',
  PeriodClosed:    'error',
  PeriodOpened:    'info',
  Created:         'default',
  Updated:         'default',
};

function formatActionType(action: string): string {
  return action.replace(/([A-Z])/g, ' $1').trim();
}

export function AuditTab() {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [changedBy, setChangedBy] = useState('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'audit', page, rowsPerPage, changedBy],
    queryFn: () => assessmentAuditApi.getAuditLogs({
      pageNumber: page + 1,
      pageSize: rowsPerPage,
      changedByName: changedBy || undefined,
    }),
  });

  return (
    <Box>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2} alignItems="center">
        <Typography variant="h6" sx={{ flex: 0 }}>Audit Log</Typography>
        <TextField
          size="small"
          label="Search by user"
          value={changedBy}
          onChange={(e) => { setChangedBy(e.target.value); setPage(0); }}
          sx={{ width: 240 }}
        />
      </Stack>

      {isLoading && <LoadingOverlay />}
      {error && <ApiErrorAlert error={error} />}

      {data && (
        <>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>When</TableCell>
                  <TableCell>Action</TableCell>
                  <TableCell>Changed By</TableCell>
                  <TableCell>Entity</TableCell>
                  <TableCell>Remarks</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.data.map((row) => (
                  <TableRow key={row.id} hover>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>
                      {new Date(row.changedOn).toLocaleString()}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={formatActionType(row.actionType)}
                        size="small"
                        color={ACTION_COLORS[row.actionType] ?? 'default'}
                      />
                    </TableCell>
                    <TableCell>{row.changedByName}</TableCell>
                    <TableCell>
                      <Typography variant="caption" color="text.secondary">
                        {row.relatedEntityType}
                      </Typography>
                    </TableCell>
                    <TableCell>{row.remarks}</TableCell>
                  </TableRow>
                ))}
                {data.data.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} align="center">
                      <Typography variant="body2" color="text.secondary" py={3}>No audit entries found.</Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={data.totalCount}
            page={page}
            onPageChange={(_, p) => setPage(p)}
            rowsPerPage={rowsPerPage}
            onRowsPerPageChange={(e) => { setRowsPerPage(+e.target.value); setPage(0); }}
            rowsPerPageOptions={[10, 25, 50, 100]}
          />
        </>
      )}
    </Box>
  );
}
