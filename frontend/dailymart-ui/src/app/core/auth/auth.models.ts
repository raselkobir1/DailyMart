export interface LoginRequest {
  username: string;
  password: string;
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
}

export interface AuthenticatedUser {
  username: string;
  fullName: string;
  role: string;
}
