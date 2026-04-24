import { Chip } from '@mui/material';
import type { SurveyStatus } from '../types';

interface SurveyStatusChipProps {
  status: SurveyStatus;
}

const STATUS_CONFIG: Record<SurveyStatus, { label: string; color: 'default' | 'success' | 'error' }> = {
  Draft:  { label: 'Draft',  color: 'default' },
  Active: { label: 'Active', color: 'success' },
  Closed: { label: 'Closed', color: 'error'   },
};

export function SurveyStatusChip({ status }: SurveyStatusChipProps) {
  const { label, color } = STATUS_CONFIG[status];
  return <Chip label={label} color={color} size="small" />;
}
