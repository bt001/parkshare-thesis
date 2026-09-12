import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

import { BookingStatus, RenterBookingItem } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';

/** Read-only booking history for the authenticated renter. */
@Component({
  selector: 'app-renter-bookings', standalone: true,
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './renter-bookings.component.html',
  styleUrl: './renter-bookings.component.scss'
})
export class RenterBookingsComponent implements OnInit, OnDestroy {
  private readonly bookingService = inject(BookingService);
  /** Active request, cancelled before replacement or destruction. */
  private request?: Subscription;
  /** Filter snapshot used by pagination, refresh, and retry. */
  private appliedStatus?: BookingStatus;
  /** Last requested page, retained when that request fails. */
  private requestedPage = 1;
  /** Status options supported by the stored booking lifecycle. */
  readonly BookingStatus = BookingStatus;
  /** Draft status; blank means all statuses. */
  readonly status = new FormControl('', { nonNullable: true });
  /** Reactive form binding for status filter submission. */
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
  ngOnInit(): void { this.loadPage(1); }
  /** Cancels retrieval when leaving this page. */
  ngOnDestroy(): void { this.request?.unsubscribe(); }

  /** Applies the draft status and starts from page one. */
  applyFilters(): void {
    this.appliedStatus = this.status.value === '' ? undefined : Number(this.status.value);
    this.loadPage(1);
  }

  /** Repeats the last request using its applied filters and page. */
  refresh(): void { this.loadPage(this.requestedPage); }

  /** Loads one page without applying draft filter changes. */
  loadPage(page: number): void {
    if (page < 1) return;
    this.request?.unsubscribe();
    this.requestedPage = page;
    this.isLoading = true;
    this.errorMessages = [];
    this.bookings = [];
    this.request = this.bookingService.getForRenter({
      page, pageSize: this.pageSize,
      ...(this.appliedStatus !== undefined ? { statuses: [this.appliedStatus] } : {})
    }).pipe(finalize(() => (this.isLoading = false))).subscribe({
      next: (result) => {
        this.bookings = result.items;
        this.page = result.page;
        this.totalCount = result.totalCount;
      },
      error: (error) => { this.errorMessages = this.getErrors(error); }
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
