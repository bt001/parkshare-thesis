import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { SessionOutcome } from '../api/auth/auth.service';

/** Accepts local destinations and prevents authentication redirect loops. */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value?.startsWith('/') || value.startsWith('//') || /[\\\u0000-\u0020]/.test(value)) return '/dashboard';
  try {
    const decoded = decodeURIComponent(value);
    if (decoded.startsWith('//') || decoded.includes('\\')
      || /^\/(login|register|session-recovery)(?:[/?#;]|$)/i.test(decoded)) return '/dashboard';
  } catch { return '/dashboard'; }
  return value;
}

/** Routes recovery failures without creating duplicate navigation loops. */
@Injectable({ providedIn: 'root' })
export class SessionNavigationService {
  /** Application router. */
  private readonly router = inject(Router);
  /** Destination currently being opened by concurrent failures. */
  private pending: string | null = null;

  /** Creates a login or recovery destination with a safe return path. */
  destination(outcome: SessionOutcome, returnUrl: string) {
    return this.router.createUrlTree([outcome === 'unavailable' ? '/session-recovery' : '/login'], {
      queryParams: { returnUrl: safeReturnUrl(returnUrl) }
    });
  }

  /** Opens a recovery failure once while concurrent requests settle. */
  showFailure(outcome: SessionOutcome): void {
    if (/^\/(login|register|session-recovery)(?:[/?#;]|$)/.test(this.router.url)) return;
    const target = this.router.serializeUrl(this.destination(outcome, this.router.url));
    if (this.pending === target) return;
    this.pending = target;
    void this.router.navigateByUrl(target).finally(() => { if (this.pending === target) this.pending = null; });
  }
}
