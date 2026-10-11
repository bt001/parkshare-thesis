import { DOCUMENT } from '@angular/common';
import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import { Subscription, distinctUntilChanged, fromEvent, interval, map } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { NotificationService } from '../api/notifications/notification.service';

/** Shell-scoped unread state; never retained across protected shell lifetimes. */
@Injectable()
export class NotificationStateService implements OnDestroy {
  private readonly api = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);
  private readonly subscriptions = new Subscription();
  private request?: Subscription;
  private started = false;
  private userId?: number;
  /** Last successful count, absent until loaded for this account. */
  readonly count = signal<number | null>(null);
  /** Whether the latest attempt failed, making any prior count stale. */
  readonly unavailable = signal(false);

  /** Starts account-aware polling once the authenticated shell is mounted. */
  start(): void {
    if (this.started) return;
    this.started = true;
    this.subscriptions.add(this.auth.session$.pipe(map(session => session?.userId), distinctUntilChanged()).subscribe(userId => {
      this.request?.unsubscribe();
      this.userId = userId;
      this.count.set(null); this.unavailable.set(false);
      this.refresh();
    }));
    this.subscriptions.add(interval(60_000).subscribe(() => {
      if (!this.document.hidden && (!this.request || this.request.closed)) this.refresh();
    }));
    this.subscriptions.add(fromEvent(this.document, 'visibilitychange').subscribe(() => {
      if (!this.document.hidden) this.refresh();
    }));
  }

  /** Replaces stale count requests after explicit refresh or mark-read. */
  refresh(): void {
    this.request?.unsubscribe();
    const userId = this.userId;
    if (!userId) return;
    this.request = this.api.unreadCount().subscribe({
      next: result => {
        if (this.auth.session?.userId !== userId) return;
        this.count.set(result.count); this.unavailable.set(false);
      },
      error: () => { if (this.auth.session?.userId === userId) this.unavailable.set(true); }
    });
  }

  /** Stops transport and timers when leaving the protected shell. */
  ngOnDestroy(): void { this.request?.unsubscribe(); this.subscriptions.unsubscribe(); this.count.set(null); }
}
