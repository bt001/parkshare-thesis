import { NotificationStateService } from '../notifications/notification-state.service';
import { AcknowledgeVisibleDirective } from '../shared/acknowledge-visible.directive';
import { DatePipe, DOCUMENT } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription, combineLatest, distinctUntilChanged, finalize, map } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { ChatMessage } from '../api/chat/chat.models';
import { ChatService } from '../api/chat/chat.service';

/** Shared participant conversation with automatic refresh. */
@Component({
  selector: 'app-booking-chat', standalone: true,
  imports: [AcknowledgeVisibleDirective, DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './booking-chat.component.html', styleUrl: './booking-chat.component.scss'
})
export class BookingChatComponent implements OnInit, OnDestroy {
  /** Activity state inherited from the authenticated shell. */
  readonly activity = inject(NotificationStateService, { optional: true });
  /** Distinct owned bookings requiring attention. */
  get ownerAttention(): number | null { return this.activity?.ownerCount?.() ?? null; }
  /** Distinct rented bookings requiring attention. */
  get renterAttention(): number | null { return this.activity?.renterCount?.() ?? null; }

  private readonly api = inject(ChatService);
  private readonly document = inject(DOCUMENT);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private context?: Subscription;
  private request?: Subscription;
  private polling?: Subscription;
  private probe?: Subscription;
  /** Newer messages available while reading older pages. */ newMessages = false;
  /** Background failure feedback while retaining messages and draft. */ refreshError = '';
  private mutation?: Subscription;
  private userId = 0;
  private ownerId = 0;
  private requestedPage = 1;
  /** Active booking identity. */ bookingId = 0;
  /** Chronological messages within the current page. */ items: ChatMessage[] = [];
  /** Current page, newest first. */ page = 1;
  /** Fixed page capacity. */ readonly pageSize = 50;
  /** Total conversation size. */ totalCount = 0;
  /** Whether participant access was verified. */ accessible = false;
  /** Pending retrieval. */ loading = false;
  /** Pending send. */ sending = false;
  /** Retrieval feedback. */ error = '';
  /** Send feedback. */ sendError = '';
  /** Unsaved plain-text draft. */ readonly draft = new FormControl('', { nonNullable: true });
  /** Return destination based on booking participation. */ backLink = '/bookings';
  /** Whether a bounded draft can be submitted. */
  get canSend(): boolean {
    const text = this.draft.value.trim();
    return this.accessible && !this.sending && text.length > 0 && text.length <= 2000;
  }
  /** Resets private state on booking or account changes. */
  ngOnInit(): void {
    this.polling = this.activity?.pulse?.subscribe(() => this.poll());
    this.context = combineLatest([
      this.route.paramMap.pipe(map(params => Number(params.get('id')))),
      this.auth.session$.pipe(map(session => session?.userId ?? 0))
    ]).pipe(distinctUntilChanged(([a, b], [c, d]) => a === c && b === d)).subscribe(([bookingId, userId]) => {
      this.request?.unsubscribe(); this.mutation?.unsubscribe(); this.probe?.unsubscribe(); this.newMessages = false;
      this.bookingId = bookingId; this.userId = userId;
      this.items = []; this.totalCount = 0; this.page = 1; this.requestedPage = 1;
      this.accessible = false; this.error = ''; this.sendError = ''; this.draft.reset();
      this.ownerId = 0; this.backLink = '/bookings';
      if (userId && Number.isSafeInteger(bookingId) && bookingId > 0) this.load(1);
      else this.error = 'Booking not found.';
    });
  }
  /** Loads a page, cancelling any superseded retrieval. */
  load(page: number, background = false): void {
    if (page < 1 || !this.userId) return;
    this.request?.unsubscribe();
    this.requestedPage = page; if (!background) { this.loading = true; this.error = ''; this.items = []; }
    if (page === 1 && !background) this.newMessages = false;
    this.request = this.api.get(this.bookingId, page).pipe(finalize(() => this.loading = false)).subscribe({
      next: result => {
        this.refreshError = ''; this.error = '';
        const lastVisible = this.document.querySelector('.conversation article:last-child')?.getBoundingClientRect();
        if (background && result.totalCount > this.totalCount && lastVisible && lastVisible.bottom > (this.document.defaultView?.innerHeight ?? 0)) {
          this.newMessages = true; return;
        }
        this.newMessages = false; this.items = [...result.items].reverse(); this.page = result.page; this.totalCount = result.totalCount;
        this.ownerId = result.ownerId; this.accessible = true;
        this.backLink = this.userId === this.ownerId ? '/owner/bookings' : '/bookings';
      },
      error: error => {
        if (error.status === 404 || error.status === 401) this.accessible = false;
        if (background) this.refreshError = 'Updates temporarily unavailable. Retrying automatically.';
        else this.error = error.status === 404 ? 'Booking not found.' : 'Messages could not be loaded. Please retry.';
      }
    });
  }
  /** Polls newest messages without replacing an older page or interrupting a send. */
  private poll(): void {
    if (!this.userId || this.sending || (this.request && !this.request.closed) || (this.probe && !this.probe.closed)) return;
    if (this.page === 1) { this.load(1, true); return; }
    this.probe = this.api.get(this.bookingId, 1).subscribe({
      next: result => { this.newMessages = result.totalCount > this.totalCount; this.refreshError = ''; },
      error: () => { this.refreshError = 'Updates temporarily unavailable. Retrying automatically.'; }
    });
  }
  /** Returns a message ID only when it was sent by the other participant. */
  incomingId(message: ChatMessage): number { return message.senderId === this.userId ? 0 : message.id; }
  /** Repeats the last requested page. */
  refresh(): void { this.load(this.requestedPage); }
  /** Sends one request and retains the draft on failure. */
  send(): void {
    if (!this.canSend) return;
    this.sending = true; this.sendError = '';
    this.mutation = this.api.send(this.bookingId, this.draft.value.trim()).pipe(finalize(() => this.sending = false)).subscribe({
      next: () => { this.draft.reset(); this.activity?.refresh(); this.load(1); },
      error: () => { this.sendError = 'Send could not be confirmed. Refresh before resending to avoid a duplicate.'; }
    });
  }
  /** Labels participants without exposing unrelated profile data. */
  senderLabel(message: ChatMessage): string {
    return message.senderId === this.userId ? 'You' : message.senderId === this.ownerId ? 'Owner' : 'Renter';
  }
  /** Cancels all conversation work on navigation. */
  ngOnDestroy(): void { this.polling?.unsubscribe(); this.probe?.unsubscribe(); this.context?.unsubscribe(); this.request?.unsubscribe(); this.mutation?.unsubscribe(); this.probe?.unsubscribe(); this.newMessages = false; }
}
