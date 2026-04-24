import { Alert, Box, Button, Card, CardContent, TextField, Typography } from '@/components/ui';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, Navigate, Link } from 'react-router-dom';
import { useAuth } from '../../shared/hooks/useAuth';
import { usePageTitle } from '../../shared/hooks/usePageTitle';

const loginSchema = z.object({
  email: z.string().email('Valid email is required'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export default function LoginPage() {
  usePageTitle('Sign In');
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) });

  if (isAuthenticated) return <Navigate to="/" replace />;

  const onSubmit = async (data: LoginFormValues) => {
    try {
      await login(data);
      navigate('/', { replace: true });
    } catch {
      setError('root', { message: 'Invalid email or password.' });
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
      <Card sx={{ width: 420, p: 2 }}>
        <CardContent>
          <Typography variant="h5" mb={1} fontWeight={700} color="primary">
            KnowHub
          </Typography>
          <Typography variant="body2" color="text.secondary" mb={3}>
            Sign in to your account
          </Typography>

          {errors.root && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {errors.root.message}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              {...register('email')}
              label="Email address"
              type="email"
              fullWidth
              autoFocus
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
              autoComplete="current-password"
              error={!!errors.password}
              helperText={errors.password?.message}
              sx={{ mb: 3 }}
            />
            <Button
              type="submit"
              variant="contained"
              fullWidth
              size="large"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </Button>

            <Typography variant="body2" align="center" mt={2} color="text.secondary">
              Don't have an account?{' '}
              <Typography component={Link} to="/register" variant="body2" color="primary">
                Create one
              </Typography>
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
