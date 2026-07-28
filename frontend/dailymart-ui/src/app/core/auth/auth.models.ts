export interface LoginRequest {
  username: string;
  password: string;
}

/** Self-service "register your shop" - creates a brand new tenant + its first Admin user. */
export interface RegisterRequest {
  companyName: string;
  username: string;
  password: string;
  fullName: string;
  email: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  username: string;
  fullName: string;
  role: string;
  tenantId: number;
  companyName: string;
}

export interface AuthenticatedUser {
  username: string;
  fullName: string;
  role: string;
  tenantId: number;
  companyName: string;
}
