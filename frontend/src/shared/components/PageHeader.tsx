import { Box, Typography, Breadcrumbs, Link as MuiLink } from '@mui/material';
import { Link } from 'react-router-dom';
import type { ReactNode } from 'react';

interface BreadcrumbItem {
  label: string;
  to?: string;
}

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  breadcrumbs?: BreadcrumbItem[];
  actions?: ReactNode;
}

export function PageHeader({ title, subtitle, breadcrumbs, actions }: PageHeaderProps) {
  return (
    <Box mb={3}>
      {breadcrumbs && breadcrumbs.length > 0 && (
        <Breadcrumbs aria-label="breadcrumb" sx={{ mb: 1 }}>
          {breadcrumbs.map((crumb, i) =>
            crumb.to ? (
              <MuiLink
                key={i}
                component={Link}
                to={crumb.to}
                underline="hover"
                color="inherit"
                sx={{ fontSize: '0.875rem' }}
              >
                {crumb.label}
              </MuiLink>
            ) : (
              <Typography key={i} color="text.primary" sx={{ fontSize: '0.875rem' }}>
                {crumb.label}
              </Typography>
            )
          )}
        </Breadcrumbs>
      )}
      <Box display="flex" alignItems="flex-start" justifyContent="space-between" gap={2}>
        <Box>
          <Typography variant="h5" component="h1">
            {title}
          </Typography>
          {subtitle && (
            <Typography variant="body2" color="text.secondary" mt={0.5}>
              {subtitle}
            </Typography>
          )}
        </Box>
        {actions && <Box flexShrink={0}>{actions}</Box>}
      </Box>
    </Box>
  );
}
