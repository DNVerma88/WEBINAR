import { Box, CircularProgress } from '@mui/material';

interface LoadingOverlayProps {
  fullPage?: boolean;
}

export function LoadingOverlay({ fullPage = false }: LoadingOverlayProps) {
  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight={fullPage ? '100vh' : '200px'}
    >
      <CircularProgress />
    </Box>
  );
}
