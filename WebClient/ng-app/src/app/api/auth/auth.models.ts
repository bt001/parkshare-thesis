export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LoginResult {
  userId: number;
  userName: string;
  email: string;
  role: string;
  name: string;
  accessToken: string;
  accessTokenExpiresAt: string;
  tokenType: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export type AuthSession = LoginResult;
