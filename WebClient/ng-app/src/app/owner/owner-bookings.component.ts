import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin, of, Subscription, switchMap } from 'rxjs';

import { BookingStatus, GetOwnerBookingsRequest, OwnerBookingItem } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';
import { OwnerListingItem } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

/** Owner decisions with server-refreshed results and applied-filter paging. */
@Component({
  selector: 'app-owner-bookings', standalone: true, imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './owner-bookings.component.html', styleUrl: './owner-workspace.scss'
})
export class OwnerBookingsComponent implements OnInit, OnDestroy {
  private readonly bookingService = inject(BookingService);
  private readonly listingService = inject(ListingService);
  private request?: Subscription;
  private decision?: Subscription;
  private appliedFilters: Omit<GetOwnerBookingsRequest, 'page' | 'pageSize'> = {};
  private requestedPage = 1;
  private listingsLoaded = false;
  readonly BookingStatus = BookingStatus;
  readonly pageSize = 20;
  readonly filters = new FormGroup({ listingId: new FormControl('', { nonNullable: true }), status: new FormControl('', { nonNullable: true }) });
  listings: OwnerListingItem[] = [];
  bookings: OwnerBookingItem[] = [];
  page = 1;
  totalCount = 0;
  isLoading = false;
  changingId?: number;
  errorMessages: string[] = [];
  /** Decision success remains visible even if the following retrieval fails. */
  successMessage = '';
  get canGoBack(): boolean { return this.page > 1 && !this.isLoading && !this.changingId; }
  get canGoForward(): boolean { return this.page * this.pageSize < this.totalCount && !this.isLoading && !this.changingId; }

  /** Loads the initial page through the same cancellable retrieval path. */
  ngOnInit(): void { this.load(); }

  /** Cancels outstanding subscriptions when navigating away. */
  ngOnDestroy(): void { this.request?.unsubscribe(); this.decision?.unsubscribe(); }

  /** Applies draft filters and resets paging. */
  applyFilters(): void {
    if (this.changingId) return;
    const value = this.filters.getRawValue();
    this.appliedFilters = {
      ...(value.listingId !== '' ? { listingId: Number(value.listingId) } : {}),
      ...(value.status !== '' ? { statuses: [Number(value.status)] } : {})
    };
    this.successMessage = '';
    this.load(1);
  }

  /** Retries retrieval without resubmitting a successful decision. */
  refresh(): void { if (!this.changingId) this.load(this.requestedPage); }

  /** Retrieves current server state and falls back when the last page disappears. */
  load(page = 1): void {
    if (page < 1) return;
    this.request?.unsubscribe();
    this.requestedPage = page;
    this.isLoading = true;
    this.errorMessages = [];
    this.bookings = [];
    const filters = { ...this.appliedFilters, pageSize: this.pageSize };
    const bookings = this.bookingService.getForOwner({ ...filters, page }).pipe(
      switchMap((result) => {
        const lastPage = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
        if (page <= lastPage) return of(result);
        this.requestedPage = lastPage;
        return this.bookingService.getForOwner({ ...filters, page: lastPage });
      })
    );
    this.request = forkJoin({
      bookings,
      listings: this.listingsLoaded ? of({ items: this.listings }) : this.listingService.getOwnerListings()
    }).pipe(finalize(() => (this.isLoading = false))).subscribe({
      next: ({ bookings, listings }) => {
        this.listings = listings.items;
        this.listingsLoaded = true;
        this.page = bookings.page;
        this.totalCount = bookings.totalCount;
        this.bookings = bookings.items;
      },
      error: (error) => { this.errorMessages = this.getErrors(error, 'Booking requests could not be loaded. Please retry.'); }
    });
  }

  /** Saves a Pending decision, then refreshes every affected row and count. */
  decide(booking: OwnerBookingItem, approve: boolean): void {
    if (booking.status !== BookingStatus.Pending || this.changingId || this.isLoading) return;
    this.changingId = booking.bookingId;
    this.errorMessages = [];
    this.successMessage = '';
    const operation = approve ? this.bookingService.approve(booking.bookingId) : this.bookingService.reject(booking.bookingId);
    this.decision = operation.pipe(finalize(() => (this.changingId = undefined))).subscribe({
      next: () => {
        this.successMessage = `Booking #${booking.bookingId} ${approve ? 'approved' : 'rejected'}.`;
        this.load(this.page);
      },
      error: (error) => { this.errorMessages = this.getErrors(error, 'The booking decision could not be saved.'); }
    });
  }

  /** Completes an eligible booking and refreshes the applied page. */
  complete(booking: OwnerBookingItem): void {
    if (!booking.canComplete || this.changingId || this.isLoading) return;
    this.changingId = booking.bookingId; this.errorMessages = []; this.successMessage = '';
    this.decision = this.bookingService.complete(booking.bookingId).pipe(finalize(() => this.changingId = undefined)).subscribe({
      next: () => { this.successMessage = `Booking #${booking.bookingId} completed.`; this.load(this.page); },
      error: error => { this.errorMessages = this.getErrors(error, 'The booking could not be completed.'); }
    });
  }

  /** Extracts safe API messages while retaining a useful retry fallback. */
  private getErrors(error: unknown, fallback: string): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error) && error.error.every((item) => typeof item === 'string')) return error.error;
      if (typeof error.error === 'string' && error.error.trim()) return [error.error];
    }
    return [fallback];
  }
}
