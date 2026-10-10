import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { AuthSession } from '../api/auth/auth.models';

/** Browser persistence key shared with storage-event listeners. */
export const sessionStorageKey = 'parkshare.auth.session';

/** Validates persisted and server-supplied session data without trusting a cast. */
export function isAuthSession(value: unknown): value is AuthSession {
  if (!value || typeof value !== 'object') return false;
  const session = value as Record<string, unknown>;
  return typeof session['userId'] === 'number' && Number.isInteger(session['userId']) && session['userId'] > 0
    && ['userName', 'email', 'role', 'name'].every(key => typeof session[key] === 'string')
    && ['accessToken', 'refreshToken'].every(key => typeof session[key] === 'string' && !!(session[key] as string).trim())
    && session['tokenType'] === 'Bearer'
    && ['accessTokenExpiresAt', 'refreshTokenExpiresAt'].every(key => typeof session[key] === 'string' && Number.isFinite(Date.parse(session[key] as string)))
    && (session['roles'] === undefined || (Array.isArray(session['roles']) && session['roles'].every(role => typeof role === 'string')));
}

/** Persists validated authentication data with an in-memory fallback. */
@Injectable({ providedIn: 'root' })
export class AuthTokenStorageService {
  /** Whether this service runs in the browser. */
  readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  /** Session retained when browser storage is unavailable. */
  private memory: AuthSession | null = null;
  /** Stops consulting storage after it refuses an operation. */
  private storageUnavailable = false;

  /** Reads validated data and discards malformed JSON or fields. */
  getSession(): AuthSession | null {
    if (!this.browser) return null;
    if (this.storageUnavailable) return this.memory;
    let raw: string | null;
    try { raw = localStorage.getItem(sessionStorageKey); }
    catch { this.storageUnavailable = true; return this.memory; }
    if (!raw) return this.memory = null;
    try {
      const value: unknown = JSON.parse(raw);
      if (isAuthSession(value)) return this.memory = value;
    } catch { /* Discard invalid persisted JSON below. */ }
    this.clear();
    return null;
  }

  /** Saves credentials without requiring persistent storage. */
  setSession(session: AuthSession): void {
    if (!this.browser) return;
    this.memory = session;
    try { localStorage.setItem(sessionStorageKey, JSON.stringify(session)); }
    catch { this.storageUnavailable = true; }
  }

  /** Removes persisted and in-memory credentials. */
  clear(): void {
    this.memory = null;
    if (this.browser) {
      try { localStorage.removeItem(sessionStorageKey); }
      catch { this.storageUnavailable = true; }
    }
  }

  /** Returns the stored access token. */
  getAccessToken(): string | null { return this.getSession()?.accessToken ?? null; }
  /** Returns the stored refresh token. */
  getRefreshToken(): string | null { return this.getSession()?.refreshToken ?? null; }
}
