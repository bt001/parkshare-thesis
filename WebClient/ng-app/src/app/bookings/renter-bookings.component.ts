import { ParkingPeriodPipe, AthensTimestampPipe } from '../shared/parking-date.pipe';
import { NotificationStateService } from '../notifications/notification-state.service';
import { ActivityBadgeComponent } from '../shared/activity-badge.component';
import { AcknowledgeVisibleDirective } from '../shared/acknowledge-visible.directive';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { distinctUntilChanged, finalize, map, Subscription } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { RatingService } from '../api/ratings/rating.service';

import { BookingStatus, RenterBookingItem } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';

/** Booking history and editable reviews for the authenticated renter. */
@Component({
  selector: 'app-renter-bookings', standalone: true,
  imports: [ParkingPeriodPipe, AthensTimestampPipe, ActivityBadgeComponent, AcknowledgeVisibleDirective, ReactiveFormsModule, RouterLink],
  templateUrl: './renter-bookings.component.html',
  styleUrl: './renter-bookings.component.scss',
  host: { class: 'ui-controls' }
})
export class RenterBookingsComponent implements OnInit, OnDestroy {
  /** Activity state inherited from the authenticated shell. */
  readonly activity = inject(NotificationStateService, { optional: true });
  /** Distinct owned bookings requiring attention. */
  get ownerAttention(): number | null { return this.activity?.ownerCount?.() ?? null; }
  /** Distinct rented bookings requiring attention. */
  get renterAttention(): number | null { return this.activity?.renterCount?.() ?? null; }

  private readonly ratingService = inject(RatingService);
  private readonly auth = inject(AuthService);
  private session?: Subscription;
  private mutation?: Subscription;
  /** Booking whose review is currently being edited. */ editingId?: number;
  /** Whether a review is being saved. */ saving = false;
  /** Feedback scoped to the review form. */ ratingError = '';
  /** Successful review feedback. */ ratingSuccess = '';
  /** Review draft retained on failed submission. */
  readonly review = new FormGroup({
    stars: new FormControl(5, { nonNullable: true }),
    comment: new FormControl('', { nonNullable: true })
  });
  /** Whether the draft is within API bounds. */
  get canSaveRating(): boolean {
    const { stars, comment } = this.review.getRawValue();
    return !this.saving && Number.isInteger(stars) && stars >= 1 && stars <= 5 && comment.trim().length <= 1000;
  }
  private readonly bookingService = inject(BookingService);
  /** Active request, cancelled before replacement or destruction. */
  private request?: Subscription;
  private polling?: Subscription;
  /** Non-blocking background refresh feedback. */ refreshError = '';
  /** Filter snapshot used by pagination, refresh, and retry. */
  private appliedStatus?: BookingStatus;
  /** Last requested page, retained when that request fails. */
  private requestedPage = 1;
  /** Status options supported by the stored booking lifecycle. */
  readonly BookingStatus = BookingStatus;
  /** Immediately applied status; blank means all statuses. */
  readonly status = new FormControl('', { nonNullable: true });
  /** Reactive form binding for the status filter. */
  readonly filters = new FormGroup({ status: this.status });
  /** Fixed number of bookings requested per page. */
  readonly pageSize = 20;
  /** Bookings returned by the latest successful request. */
  bookings: RenterBookingItem[] = [];
  /** Current successful page. */
  page = 1;
  /** Total bookings matching the applied filter. */
  totalCount = 0;
  /** Whether a request is pending. */
  isLoading = false;
  /** User-visible retrieval errors. */
  errorMessages: string[] = [];

  /** Whether a status filter is applied. */
  get hasFilter(): boolean { return this.appliedStatus !== undefined; }
  /** Whether an earlier page is available. */
  get canGoBack(): boolean { return this.page > 1 && !this.isLoading; }
  /** Whether a later page is available. */
  get canGoForward(): boolean { return this.page * this.pageSize < this.totalCount && !this.isLoading; }

  /** Loads all statuses on entry. */
  ngOnInit(): void {
    this.polling = this.activity?.pulse?.subscribe(() => { if (!this.saving && !this.editingId && (!this.request || this.request.closed)) this.loadPage(this.page, true); });
    this.session = this.auth.session$.pipe(map(session => session?.userId ?? 0), distinctUntilChanged()).subscribe(userId => {
      this.request?.unsubscribe(); this.mutation?.unsubscribe();
      this.cancelRating(); this.bookings = []; this.totalCount = 0; this.page = 1;
      this.ratingSuccess = ''; this.errorMessages = []; this.status.reset(); this.appliedStatus = undefined;
      if (userId) this.loadPage(1);
    });
  }
  /** Cancels retrieval when leaving this page. */
  ngOnDestroy(): void { this.polling?.unsubscribe(); this.request?.unsubscribe(); this.mutation?.unsubscribe(); this.session?.unsubscribe(); this.cancelRating(); }

  /** Applies the selected status and starts from page one. */
  applyFilters(): void {
    if (this.saving) return;
    this.appliedStatus = this.status.value === '' ? undefined : Number(this.status.value);
    this.loadPage(1);
  }

  /** Repeats the last request using its applied filters and page. */
  refresh(): void { this.loadPage(this.requestedPage); }

  /** Loads one page without applying draft filter changes. */
  loadPage(page: number, background = false): void {
    if (page < 1 || this.saving) return;
    if (!background) this.cancelRating();
    this.request?.unsubscribe();
    this.requestedPage = page;
    if (!background) { this.isLoading = true; this.errorMessages = []; this.bookings = []; }
    this.request = this.bookingService.getForRenter({
      page, pageSize: this.pageSize,
      ...(this.appliedStatus !== undefined ? { statuses: [this.appliedStatus] } : {})
    }).pipe(finalize(() => (this.isLoading = false))).subscribe({
      next: (result) => {
        this.refreshError = ''; this.errorMessages = [];
        this.bookings = result.items;
        this.page = result.page;
        this.totalCount = result.totalCount;
      },
      error: (error) => { if (background) this.refreshError = 'Updates temporarily unavailable. Retrying automatically.'; else this.errorMessages = this.getErrors(error); }
    });
  }

  /** Starts editing the saved review or a new five-star draft. */
  editRating(booking: RenterBookingItem): void {
    if (this.saving || booking.status !== BookingStatus.Completed) return;
    this.editingId = booking.bookingId; this.ratingError = ''; this.ratingSuccess = '';
    this.review.setValue({ stars: booking.rating?.stars ?? 5, comment: booking.rating?.comment ?? '' });
  }
  /** Discards draft changes; the saved review remains authoritative. */
  cancelRating(): void {
    this.editingId = undefined; this.ratingError = ''; this.review.reset();
  }
  /** Saves the draft without clearing it on failure. */
  saveRating(booking: RenterBookingItem): void {
    if (!this.canSaveRating || this.editingId !== booking.bookingId) return;
    this.saving = true; this.ratingError = ''; this.ratingSuccess = '';
    const { stars, comment } = this.review.getRawValue();
    this.mutation = this.ratingService.save(booking.bookingId, stars, comment.trim()).pipe(finalize(() => this.saving = false)).subscribe({
      next: rating => { booking.rating = rating; this.cancelRating(); this.ratingSuccess = 'Rating saved.'; },
      error: () => { this.ratingError = 'Your rating could not be saved. Please retry.'; }
    });
  }

  /** Converts API failures into readable feedback. */
  private getErrors(error: unknown): string[] {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) return ['Your session has expired. Please log in again.'];
      if (Array.isArray(error.error) && error.error.length && error.error.every((item) => typeof item === 'string')) return error.error;
      if (typeof error.error === 'string' && error.error.trim()) return [error.error];
    }
    return ['Booking history could not be loaded. Please try again.'];
  }
}
