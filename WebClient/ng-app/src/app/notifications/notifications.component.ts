import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription, distinctUntilChanged, finalize, map, of, switchMap } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { NotificationItem } from '../api/notifications/notification.models';
import { NotificationService } from '../api/notifications/notification.service';
import { NotificationStateService } from './notification-state.service';

/** Recipient inbox with explicit refresh and persisted read state. */
@Component({
  selector: 'app-notifications', standalone: true, imports: [DatePipe, RouterLink],
  templateUrl: './notifications.component.html', styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit, OnDestroy {
  private readonly api = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly state = inject(NotificationStateService);
  private request?: Subscription;
  private mutation?: Subscription;
  private session?: Subscription;
  private requestedPage = 1;
  /** Visible events. */
  items: NotificationItem[] = [];
  /** Current page. */
  page = 1;
  /** Matching event count. */
  totalCount = 0;
  /** Active unread-only filter. */
  unreadOnly = false;
  /** Whether inbox retrieval is pending. */
  loading = false;
  /** Retrieval error shown instead of a misleading empty state. */
  error = '';
  /** Mutation error retained independently from inbox retrieval. */
  readError = '';
  /** Event whose read update is pending. */
  readingId: number | null = null;
  /** Loads the inbox and clears data when account identity changes. */
  ngOnInit(): void {
    this.session = this.auth.session$.pipe(map(session => session?.userId), distinctUntilChanged()).subscribe(userId => {
      this.request?.unsubscribe(); this.mutation?.unsubscribe();
      this.items = []; this.totalCount = 0; this.page = 1; this.unreadOnly = false;
      this.error = ''; this.readError = ''; this.readingId = null;
      if (userId) this.load(1);
    });
  }
  /** Applies a filter and returns to its first page. */
  filter(unreadOnly: boolean): void { this.unreadOnly = unreadOnly; this.readError = ''; this.load(1); }
  /** Refreshes or retries the last requested page and unread count. */
  refresh(): void { this.state.refresh(); this.load(this.requestedPage); }
  /** Cancels superseded reads and falls back when the last unread page disappears. */
  load(page: number): void {
    this.request?.unsubscribe();
    this.requestedPage = page; this.loading = true; this.error = ''; this.items = [];
    const userId = this.auth.session?.userId;
    const unreadOnly = this.unreadOnly;
    this.request = this.api.get(page, unreadOnly).pipe(
      switchMap(result => {
        const lastPage = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
        if (page <= lastPage) return of(result);
        this.requestedPage = lastPage;
        return this.api.get(lastPage, unreadOnly);
      }),
      finalize(() => this.loading = false)
    ).subscribe({
      next: result => {
        if (this.auth.session?.userId !== userId) return;
        this.items = result.items; this.page = result.page; this.totalCount = result.totalCount;
      },
      error: () => { if (this.auth.session?.userId === userId) this.error = 'Notifications could not be loaded. Please retry.'; }
    });
  }
  /** Persists read state, then reconciles inbox pagination and the shell badge. */
  markRead(item: NotificationItem): void {
    if (this.readingId !== null || item.isRead) return;
    this.readingId = item.id; this.readError = '';
    const userId = this.auth.session?.userId;
    this.mutation = this.api.markRead(item.id).pipe(finalize(() => this.readingId = null)).subscribe({
      next: () => {
        if (this.auth.session?.userId !== userId) return;
        this.refresh();
      },
      error: () => { if (this.auth.session?.userId === userId) this.readError = 'The notification could not be marked read. Please retry.'; }
    });
  }
  /** Resolves only known booking events to existing booking lists. */
  bookingLink(item: NotificationItem): string | null {
    if (!item.bookingId) return null;
    if (item.type === 'booking-requested') return '/owner/bookings';
    return ['booking-approved', 'booking-rejected', 'booking-overlap-rejected'].includes(item.type) ? '/bookings' : null;
  }
  /** Cancels all outstanding account work on navigation. */
  ngOnDestroy(): void { this.request?.unsubscribe(); this.mutation?.unsubscribe(); this.session?.unsubscribe(); }
}
