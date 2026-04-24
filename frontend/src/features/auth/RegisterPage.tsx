import { Alert, Box, Button, Card, CardContent, Stack, TextField, Typography } from '@/components/ui';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, Navigate, Link } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { authApi } from '../../shared/api/auth';
import { useAuth } from '../../shared/hooks/useAuth';
import { usePageTitle } from '../../shared/hooks/usePageTitle';

const registerSchema = z.object({
  fullName: z.string().min(1, 'Full name is required').max(200),
  email: z.string().email('Valid email is required'),
  password: z.string().min(12, 'Password must be at least 12 characters'),
  tenantSlug: z.string().min(1, 'Tenant is required'),
  department: z.string().max(100).optional().or(z.literal('')),
  designation: z.string().max(100).optional().or(z.literal('')),
  location: z.string().max(100).optional().or(z.literal('')),
});

type RegisterFormValues = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  usePageTitle('Create Account');
  const navigate = useNavigate();
  const { isAuthenticated, login } = useAuth();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) });

  const registerMutation = useMutation({
    mutationFn: authApi.register,
  });

  if (isAuthenticated) return <Navigate to="/" replace />;

  const onSubmit = async (data: RegisterFormValues) => {
    try {
      await registerMutation.mutateAsync({
        fullName: data.fullName,
        email: data.email,
        password: data.password,
        tenantSlug: data.tenantSlug,
        department: data.department || undefined,
        designation: data.designation || undefined,
        location: data.location || undefined,
      });
      // After registration, log in directly
      await login({ email: data.email, password: data.password });
      navigate('/', { replace: true });
    } catch {
      setError('root', { message: 'Registration failed. Please check your details and try again.' });
    }
  };

  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight="100vh"
      bgcolor="background.default"
    >
      <Card sx={{ width: 480, p: 2 }}>
        <CardContent>
          <Typography variant="h5" mb={1} fontWeight={700} color="primary">
            KnowHub
          </Typography>
          <Typography variant="body2" color="text.secondary" mb={3}>
            Create your account
          </Typography>

          {errors.root && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {errors.root.message}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              {...register('fullName')}
              label="Full Name"
              fullWidth
              autoFocus
              autoComplete="name"
              error={!!errors.fullName}
              helperText={errors.fullName?.message}
              sx={{ mb: 2 }}
            />

            <TextField
              {...register('email')}
              label="Email address"
              type="email"
              fullWidth
              autoComplete="email"
              error={!!errors.email}
              helperText={errors.email?.message}
              sx={{ mb: 2 }}
            />

            <TextField
              {...register('password')}
              label="Password"
              type="password"
              fullWidth
              autoComplete="new-password"
              error={!!errors.password}
              helperText={errors.password?.message}
              sx={{ mb: 2 }}
            />

            <TextField
              {...register('tenantSlug')}
              label="Organisation Slug"
              fullWidth
              error={!!errors.tenantSlug}
              helperText={errors.tenantSlug?.message ?? 'Contact your admin for the organisation identifier'}
              sx={{ mb: 2 }}
            />

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
              <TextField
                {...register('department')}
                label="Department (optional)"
                fullWidth
                error={!!errors.department}
                helperText={errors.department?.message as string}
              />
              <TextField
                {...register('designation')}
                label="Designation (optional)"
                fullWidth
                error={!!errors.designation}
                helperText={errors.designation?.message as string}
              />
            </Stack>

            <TextField
              {...register('location')}
              label="Location (optional)"
              fullWidth
              error={!!errors.location}
              helperText={errors.location?.message as string}
              sx={{ mb: 3 }}
            />

            <Button
              type="submit"
              variant="contained"
              fullWidth
              size="large"
              disabled={isSubmitting || registerMutation.isPending}
            >
              {isSubmitting ? 'Creating account…' : 'Create Account'}
            </Button>

            <Typography variant="body2" align="center" mt={2} color="text.secondary">
              Already have an account?{' '}
              <Typography component={Link} to="/login" variant="body2" color="primary">
                Sign in
              </Typography>
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
