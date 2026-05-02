import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';

import { AuthSession } from '../api/auth/auth.models';

const sessionStorageKey = 'parkshare.auth.session';

@Injectable({ providedIn: 'root' })
export class AuthTokenStorageService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly browser = isPlatformBrowser(this.platformId);

  getSession(): AuthSession | null {
    if (!this.browser) {
      return null;
    }

    const rawSession = localStorage.getItem(sessionStorageKey);
    if (!rawSession) {
      return null;
    }

    try {
      return JSON.parse(rawSession) as AuthSession;
    } catch {
      this.clear();
      return null;
    }
  }

  setSession(session: AuthSession): void {
    if (!this.browser) {
      return;
    }

    localStorage.setItem(sessionStorageKey, JSON.stringify(session));
  }

  clear(): void {
    if (!this.browser) {
      return;
    }

    localStorage.removeItem(sessionStorageKey);
  }

  getAccessToken(): string | null {
    return this.getSession()?.accessToken ?? null;
  }

  getRefreshToken(): string | null {
    return this.getSession()?.refreshToken ?? null;
  }
}
