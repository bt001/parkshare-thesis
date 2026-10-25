import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { safeReturnUrl, SessionNavigationService } from './session-navigation.service';

/** Provides an explicit retry after a temporary session recovery failure. */
@Component({
  selector: 'app-session-recovery',
  standalone: true,
  template: `<section class="login-page"><div class="login-card">
    <p class="eyebrow">ParkShare</p><h1>Reconnect to your session</h1>
    <p class="support-copy" role="status">We could not restore your session right now. Check your connection and try again.</p>
    <div class="login-form"><button type="button" [disabled]="isRecovering" (click)="retry()">{{ isRecovering ? 'Reconnecting...' : 'Retry' }}</button>
    <button class="secondary" type="button" (click)="signIn()">Sign in</button>
  </div></div></section>`,
  styleUrl: './login/login.component.scss'
})
export class SessionRecoveryComponent {
  /** Authentication coordinator. */
  private readonly auth = inject(AuthService);
  /** Current return destination. */
  private readonly route = inject(ActivatedRoute);
  /** Application navigation. */
  private readonly router = inject(Router);
  /** Shared failure destinations. */
  private readonly navigation = inject(SessionNavigationService);
  /** Cancels view callbacks when navigation destroys the screen. */
  private readonly destroyRef = inject(DestroyRef);
  /** Prevents duplicate retry actions. */
  isRecovering = false;
  /** Sanitized destination retained across recovery and login. */
  private get returnUrl(): string { return safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')); }

  /** Tries once, keeping the screen open on temporary failure. */
  retry(): void {
    if (this.isRecovering) return;
    this.isRecovering = true;
    this.auth.ensureSession().pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isRecovering = false)).subscribe({
      next: outcome => {
        if (outcome === 'authenticated') void this.router.navigateByUrl(this.returnUrl);
        else if (outcome === 'signed-out') void this.router.navigateByUrl(this.navigation.destination(outcome, this.returnUrl));
      },
      error: () => { /* A newer authentication action owns navigation. */ }
    });
  }

  /** Explicitly abandons recovery before opening the login form. */
  signIn(): void {
    this.auth.logout();
    void this.router.navigateByUrl(this.navigation.destination('signed-out', this.returnUrl));
  }
}
