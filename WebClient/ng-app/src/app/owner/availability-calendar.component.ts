import { DatePipe } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subscription, expand, reduce, throwError } from 'rxjs';
import { AvailabilityWindow } from '../api/availability/availability.models';
import { BookingStatus, OwnerBookingItem } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';
import { romanianDateTime } from '../services/booking-time';

/** A wall-clock period touching a calendar day. */
interface CalendarPeriod {
  /** Human-readable category. */
  label: string;
  /** Romanian wall-clock start. */
  start: string;
  /** Exclusive Romanian wall-clock end. */
  end: string;
  /** Optional owner note. */
  note?: string | null;
}

/** Read-only monthly overview of the owner's published schedule. */
@Component({
  selector: 'app-availability-calendar', standalone: true, imports: [DatePipe],
  templateUrl: './availability-calendar.component.html', styleUrl: './availability-calendar.component.scss'
})
export class AvailabilityCalendarComponent implements OnChanges {
  /** Listing whose confirmed bookings are retrieved. */
  @Input({ required: true }) listingId!: number;
  /** Whether open windows are unnecessary. */
  @Input() alwaysAvailable = false;
  /** Current published windows; replacing them refreshes bookings. */
  @Input() windows: AvailabilityWindow[] = [];
  /** Hides stale schedule data during retrieval or after window retrieval failure. */
  @Input() unavailable = false;
  /** Requests a fresh window snapshot from the parent. */
  @Output() refresh = new EventEmitter<void>();
  /** Selected Romanian calendar date. */
  selected = romanianDateTime().slice(0, 10);
  /** First date of the visible month. */
  month = this.selected.slice(0, 7) + '-01';
  /** Monday-first weekday labels. */
  readonly weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  /** Complete confirmed-booking snapshot. */
  bookings: OwnerBookingItem[] = [];
  /** Whether bookings are being retrieved. */
  loading = false;
  /** Safe retrieval feedback. */
  error = '';
  /** Existing owner-scoped API client. */
  private readonly service = inject(BookingService);
  /** Component lifetime used to cancel HTTP work. */
  private readonly destroyRef = inject(DestroyRef);
  /** Current snapshot request, replaced on refresh. */
  private request?: Subscription;

  /** Refreshes the booking overlay when the window snapshot changes. */
  ngOnChanges(changes: import('@angular/core').SimpleChanges): void {
    if (changes['listingId'] || changes['windows']) this.loadBookings();
  }

  /** Loads every page before publishing any booking overlay. */
  loadBookings(): void {
    this.request?.unsubscribe();
    this.loading = true; this.error = ''; this.bookings = [];
    const fetch = (page: number) => this.service.getForOwner({ listingId: this.listingId, statuses: [BookingStatus.Confirmed], page, pageSize: 100 });
    this.request = fetch(1).pipe(
      expand(page => page.page * page.pageSize < page.totalCount ? (page.items.length ? fetch(page.page + 1) : throwError(() => new Error('Incomplete booking page'))) : EMPTY, 1),
      reduce((items, page) => [...items, ...page.items], [] as OwnerBookingItem[]),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: items => { this.bookings = items.filter(item => item.status === BookingStatus.Confirmed && item.listingId === this.listingId); this.loading = false; },
      error: () => { this.error = 'Confirmed bookings could not be loaded. The calendar is incomplete.'; this.loading = false; }
    });
  }

  /** Moves one month and selects its first day. */
  moveMonth(offset: number): void {
    const date = new Date(this.month + 'T00:00:00Z');
    date.setUTCMonth(date.getUTCMonth() + offset);
    this.month = date.toISOString().slice(0, 10); this.selected = this.month;
  }

  /** Returns to the current Romanian day. */
  today(): void { this.selected = romanianDateTime().slice(0, 10); this.month = this.selected.slice(0, 7) + '-01'; }

  /** Monday-aligned cells, with empty padding outside this month. */
  get days(): string[] {
    const first = new Date(this.month + 'T00:00:00Z');
    const count = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth() + 1, 0)).getUTCDate();
    const cells = Array<string>((first.getUTCDay() + 6) % 7).fill('');
    for (let day = 1; day <= count; day++) cells.push(this.month.slice(0, 8) + String(day).padStart(2, '0'));
    while (cells.length % 7) cells.push('');
    return cells;
  }

  /** Returns intersecting periods without treating a partial day as fully available. */
  periods(day: string): CalendarPeriod[] {
    if (!day || this.unavailable) return [];
    const next = new Date(day + 'T00:00:00Z'); next.setUTCDate(next.getUTCDate() + 1);
    const start = day + 'T00:00:00'; const end = next.toISOString().slice(0, 10) + 'T00:00:00';
    return [
      ...this.windows.map(w => ({ label: w.isBlocked ? 'Blocked' : 'Open', start: w.startDateTime, end: w.endDateTime, note: w.note })),
      ...this.bookings.map(b => ({ label: 'Confirmed', start: b.startUtc, end: b.endUtc }))
    ].filter(p => p.start < end && p.end.replace(/\.0+$/, '') > start).sort((a, b) => a.start.localeCompare(b.start));
  }

  /** Distinct text labels for compact day indicators and accessible names. */
  labels(day: string): string { return [...new Set(this.periods(day).map(p => p.label))].join(', '); }
}


