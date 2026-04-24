import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Box, Button, Typography } from '@mui/material';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

/**
 * React error boundary that catches unhandled render-phase errors and displays
 * a user-friendly fallback instead of a blank screen.
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // Surface to console so dev-tools / Serilog log-ingestion can capture it.
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <Box
          display="flex"
          flexDirection="column"
          alignItems="center"
          justifyContent="center"
          minHeight="60vh"
          gap={2}
          px={3}
        >
          <Typography variant="h5" fontWeight={600}>
            Something went wrong
          </Typography>
          <Typography variant="body2" color="text.secondary" textAlign="center" maxWidth={400}>
            An unexpected error occurred. Please try refreshing the page. If the problem persists,
            contact support.
          </Typography>
          <Button variant="contained" onClick={this.handleReset}>
            Try again
          </Button>
        </Box>
      );
    }

    return this.props.children;
  }
}
