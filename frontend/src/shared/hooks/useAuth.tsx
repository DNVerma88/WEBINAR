import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import type { LoginResponse } from '../types';
import { UserRole } from '../types';
import { tokenStorage } from '../api/axiosClient';
import { authApi } from '../api/auth';
import type { LoginRequest } from '../types';

interface AuthUser {
  userId: string;
  fullName: string;
  email: string;
  role: number;
}

/** The API serializes enums as strings (JsonStringEnumConverter). Convert back to numeric flag. */
function parseRole(raw: string | number): number {
  if (typeof raw === 'number') return raw;
  const numeric = UserRole[raw as keyof typeof UserRole];
  return typeof numeric === 'number' ? numeric : 0;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function parseStoredUser(): AuthUser | null {
  const raw = sessionStorage.getItem('kh_user');
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(parseStoredUser);

  const login = useCallback(async (data: LoginRequest) => {
    const response: LoginResponse = await authApi.login(data);
    tokenStorage.setAccessToken(response.accessToken);
    tokenStorage.setRefreshToken(response.refreshToken);
    const authUser: AuthUser = {
      userId: response.user.id,
      fullName: response.user.fullName,
      email: response.user.email,
      role: parseRole(response.user.role),
    };
    sessionStorage.setItem('kh_user', JSON.stringify(authUser));
    setUser(authUser);
  }, []);

  const logout = useCallback(() => {
    // Best-effort server-side token revocation; errors are swallowed so local
    // state is always cleared regardless of network conditions.
    authApi.logout().catch(() => undefined);
    tokenStorage.clear();
    sessionStorage.removeItem('kh_user');
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (role: UserRole) => {
      if (!user) return false;
      // Defensively normalise in case an old session stored the string form
      const numeric = parseRole(user.role);
      return (numeric & role) !== 0;
    },
    [user]
  );

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout, hasRole }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
