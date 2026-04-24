export enum UserRole {
  Employee = 1,
  Contributor = 2,
  Manager = 4,
  KnowledgeTeam = 8,
  Admin = 16,
  SuperAdmin = 32,
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    fullName: string;
    email: string;
    role: number | string;  // API returns string via JsonStringEnumConverter
    isActive: boolean;
    department?: string;
    designation?: string;
    location?: string;
    profilePhotoUrl?: string;
  };
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  tenantSlug: string;
  department?: string;
  designation?: string;
  location?: string;
}

export interface RegisterResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    fullName: string;
    email: string;
    role: number;
    isActive: boolean;
  };
}
