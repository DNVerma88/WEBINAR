import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../shared/hooks/useAuth';
import type { UserRole } from '../shared/types';

interface PrivateRouteProps {
  requiredRole?: UserRole;
  /** F2/F8: gate a group of routes to any one of these roles */
  requiredRoles?: UserRole[];
}

export function PrivateRoute({ requiredRole, requiredRoles }: PrivateRouteProps) {
  const { isAuthenticated, hasRole } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && !hasRole(requiredRole)) {
    return <Navigate to="/" replace />;
  }

  if (requiredRoles && requiredRoles.length > 0 && !requiredRoles.some((r) => hasRole(r))) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
