import { AuthSession } from './auth.models';

/** Creates deterministic session data for authentication regression tests. */
export function testSession(overrides: Partial<AuthSession> = {}): AuthSession {
  return {
    userId: 1, userName: 'renter', email: 'renter@example.test', name: 'Test Renter',
    role: 'Renter', roles: ['Renter'], accessToken: 'access', refreshToken: 'refresh', tokenType: 'Bearer',
    accessTokenExpiresAt: '2099-01-01T00:00:00Z', refreshTokenExpiresAt: '2099-02-01T00:00:00Z', ...overrides
  };
}
