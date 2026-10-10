import { HttpContextToken } from '@angular/common/http';
import { environment } from '../../environments/environment';

/** Marks public API reads that must not depend on browser authentication. */
export const ANONYMOUS_API = new HttpContextToken<boolean>(() => false);

/** Resolves only relative API paths, preserving external and resource URLs. */
export function resolveApiUrl(url: string): string {
  return /^\/?api\//i.test(url) ? new URL(url.replace(/^\//, ''), environment.apiUrl).href : url;
}

/** Returns the normalized API path only for the configured API origin and base. */
export function apiPath(url: string): string | null {
  try {
    const base = new URL(environment.apiUrl);
    const parsed = new URL(resolveApiUrl(url));
    const prefix = base.pathname.replace(/\/?$/, '/') + 'api/';
    if (parsed.origin !== base.origin || !parsed.pathname.toLowerCase().startsWith(prefix.toLowerCase())
      || parsed.username || parsed.password) return null;
    return parsed.pathname.slice(prefix.length).replace(/\/+$/, '').toLowerCase();
  } catch { return null; }
}

/** Recognizes anonymous authentication endpoints independently of query strings. */
export function isAuthPath(path: string): boolean { return ['auth/login', 'auth/register', 'auth/refresh'].includes(path); }
