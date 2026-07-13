import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { BookingStatus, OwnerBookingItem } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';
import { OwnerListingItem } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

@Component({
  selector: 'app-owner-bookings', standalone: true, imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './owner-bookings.component.html', styleUrl: './owner-workspace.scss'
})
export class OwnerBookingsComponent implements OnInit {
  private readonly bookingService = inject(BookingService);
  private readonly listingService = inject(ListingService);
  readonly BookingStatus = BookingStatus;
  readonly pageSize = 20;
  readonly filters = new FormGroup({ listingId: new FormControl('', { nonNullable: true }), status: new FormControl('', { nonNullable: true }) });

  listings: OwnerListingItem[] = [];
  bookings: OwnerBookingItem[] = [];
  page = 1; totalCount = 0; isLoading = false; changingId?: number; errorMessages: string[] = [];
  get canGoBack(): boolean { return this.page > 1 && !this.isLoading; }
  get canGoForward(): boolean { return this.page * this.pageSize < this.totalCount && !this.isLoading; }

  ngOnInit(): void {
    forkJoin({ listings: this.listingService.getOwnerListings(), bookings: this.bookingService.getForOwner({ page: 1, pageSize: this.pageSize }) })
      .subscribe({ next: ({ listings, bookings }) => { this.listings = listings.items; this.applyResult(bookings); }, error: (error) => (this.errorMessages = this.getErrors(error, 'Booking requests could not be loaded.')) });
  }

  load(page = 1): void {
    this.isLoading = true; this.errorMessages = [];
    const value = this.filters.getRawValue();
    this.bookingService.getForOwner({ page, pageSize: this.pageSize, listingId: value.listingId ? Number(value.listingId) : undefined, statuses: value.status ? [Number(value.status)] : undefined })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({ next: (result) => this.applyResult(result), error: (error) => (this.errorMessages = this.getErrors(error, 'Booking requests could not be loaded.')) });
  }

  decide(booking: OwnerBookingItem, approve: boolean): void {
    if (booking.status !== BookingStatus.Pending || this.changingId) return;
    this.changingId = booking.bookingId; this.errorMessages = [];
    const operation = approve ? this.bookingService.approve(booking.bookingId) : this.bookingService.reject(booking.bookingId);
    operation.pipe(finalize(() => (this.changingId = undefined))).subscribe({
      next: (result) => { booking.status = result.status; booking.statusText = result.statusText; },
      error: (error) => (this.errorMessages = this.getErrors(error, `The booking could not be ${approve ? 'approved' : 'rejected'}.`))
    });
  }

  private applyResult(result: { page: number; totalCount: number; items: OwnerBookingItem[] }): void { this.page = result.page; this.totalCount = result.totalCount; this.bookings = result.items; }
  private getErrors(error: unknown, fallback: string): string[] {
    if (error instanceof HttpErrorResponse) { if (Array.isArray(error.error)) return error.error; if (typeof error.error === 'string' && error.error.trim()) return [error.error]; if (error.message) return [error.message]; }
    return [fallback];
  }
}
