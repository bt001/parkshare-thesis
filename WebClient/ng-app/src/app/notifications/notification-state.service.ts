import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, OnDestroy, PLATFORM_ID, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, Subscription, distinctUntilChanged, fromEvent, interval, map } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';

/** Account-scoped navigation activity, refresh ticks and exact read acknowledgements. */
@Injectable()
export class NotificationStateService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly subscriptions = new Subscription();
  private reads = new Subscription();
  private request?: Subscription;
  private started = false;
  private userId?: number;
  private readonly acknowledged = new Set<string>();
  private readonly pending = new Set<string>();
  /** Visible-page refresh pulses shared by the authenticated shell. */
  readonly pulse = new Subject<void>();
  /** Account changes invalidate page-owned requests and private drafts. */
  readonly accountChanged = new Subject<number | undefined>();
  /** Last successful notification count; null until loaded. */
  readonly count = signal<number | null>(null);
  /** Distinct renter bookings requiring attention. */
  readonly renterCount = signal<number | null>(null);
  /** Distinct owner bookings requiring attention. */
  readonly ownerCount = signal<number | null>(null);
  /** Latest summary or acknowledgement failed; retained counts may be stale. */
  readonly unavailable = signal(false);
  /** Whether displayed information can be acknowledged in this tab. */
  get canAcknowledge(): boolean { return this.browser && !this.document.hidden && this.document.hasFocus(); }
  /** Starts one visible-tab poller and clears private state on identity changes. */
  start(): void {
    if (this.started || !this.browser) return;
    this.started = true;
    this.subscriptions.add(this.auth.session$.pipe(map(s => s?.userId), distinctUntilChanged()).subscribe(id => {
      this.request?.unsubscribe(); this.reads.unsubscribe(); this.reads = new Subscription();
      this.userId = id; this.acknowledged.clear(); this.pending.clear();
      this.count.set(null); this.ownerCount.set(null); this.renterCount.set(null); this.unavailable.set(false);
      this.accountChanged.next(id); this.refresh();
    }));
    this.subscriptions.add(interval(5000).subscribe(() => this.tick()));
    this.subscriptions.add(fromEvent(this.document, 'visibilitychange').subscribe(() => {
      if (this.document.hidden) { this.request?.unsubscribe(); return; }
      this.tick();
    }));
    if (this.document.defaultView) this.subscriptions.add(fromEvent(this.document.defaultView, 'focus').subscribe(() => this.tick()));
  }
  /** Refreshes visible pages and counts without overlapping summary requests. */
  private tick(): void { if (!this.document.hidden && this.userId) { this.refresh(); this.pulse.next(); } }
  /** Retrieves fresh counts after mutations or on the periodic tick. */
  refresh(): void {
    const id = this.auth.session?.userId;
    if (!this.browser || this.document.hidden || !id || (this.request && !this.request.closed)) return;
    this.request = this.http.get<{ unreadNotificationCount: number; ownerAttentionCount: number; renterAttentionCount: number }>('api/activity/summary').subscribe({
      next: value => { if (this.auth.session?.userId !== id) return;
        this.count.set(value.unreadNotificationCount); this.ownerCount.set(value.ownerAttentionCount); this.renterCount.set(value.renterAttentionCount); this.unavailable.set(false); },
      error: () => { if (this.auth.session?.userId === id) this.unavailable.set(true); }
    });
  }
  /** Acknowledges the exact visible notification IDs, never a whole booking history. */
  acknowledgeNotifications(ids: readonly number[]): void { for (const id of ids) this.acknowledge('n' + id, `api/notification/${id}/read`, 'PATCH', {}); }
  /** Acknowledges one rendered incoming message in the active conversation. */
  acknowledgeMessage(bookingId: number, messageId: number): void { this.acknowledge('m' + messageId, `api/booking/${bookingId}/messages/read`, 'POST', { messageIds: [messageId] }); }
  /** Deduplicates successful and in-flight acknowledgements within this account session. */
  private acknowledge(key: string, url: string, method: string, body: object): void {
    const userId = this.auth.session?.userId;
    if (!userId || !this.canAcknowledge || this.pending.has(key) || this.acknowledged.has(key)) return;
    this.pending.add(key);
    this.reads.add(this.http.request(method, url, { body }).subscribe({
      next: () => { if (this.auth.session?.userId !== userId) return; this.pending.delete(key); this.acknowledged.add(key); this.request?.unsubscribe(); this.refresh(); },
      error: () => { if (this.auth.session?.userId !== userId) return; this.pending.delete(key); this.unavailable.set(true); }
    }));
  }
  /** Cancels private work when leaving the protected shell. */
  ngOnDestroy(): void { this.request?.unsubscribe(); this.reads.unsubscribe(); this.subscriptions.unsubscribe(); this.pulse.complete(); this.accountChanged.complete(); this.count.set(null); this.ownerCount.set(null); this.renterCount.set(null); }
}
