export interface PlatformLoginRequest {
  username: string;
  password: string;
}

export interface PlatformAuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  username: string;
  fullName: string;
}

export interface AuthenticatedPlatformAdmin {
  username: string;
  fullName: string;
}
