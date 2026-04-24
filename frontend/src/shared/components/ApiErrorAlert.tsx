import { Alert, Box } from '@mui/material';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '../types';

interface ApiErrorAlertProps {
  error: unknown;
}

export function ApiErrorAlert({ error }: ApiErrorAlertProps) {
  if (!error) return null;

  const axiosError = error as AxiosError<ProblemDetails>;
  const detail =
    axiosError.response?.data?.detail ??
    axiosError.response?.data?.title ??
    (error instanceof Error ? error.message : 'An unexpected error occurred.');

  const fieldErrors = axiosError.response?.data?.errors;

  return (
    <Box mb={2}>
      <Alert severity="error">
        {detail}
        {fieldErrors && (
          <ul style={{ margin: '4px 0 0 0', paddingLeft: '20px' }}>
            {Object.entries(fieldErrors).map(([field, messages]) =>
              messages.map((msg, i) => (
                <li key={`${field}-${i}`}>
                  <strong>{field}:</strong> {msg}
                </li>
              ))
            )}
          </ul>
        )}
      </Alert>
    </Box>
  );
}
