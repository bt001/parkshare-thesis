import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { AvailabilityWindow } from '../api/availability/availability.models';
import { AvailabilityService } from '../api/availability/availability.service';
import { BookingResult } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';
import { ListingDetails } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

import { bookingTimeError, nextRomanianMinute } from '../services/booking-time';

interface BookingForm {
  startDateTime: FormControl<string>;
  endDateTime: FormControl<string>;
}

@Component({
  selector: 'app-listing-detail',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './listing-detail.component.html',
  styleUrl: './listing-detail.component.scss'
})
export class ListingDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly listingService = inject(ListingService);
  private readonly availabilityService = inject(AvailabilityService);
  private readonly bookingService = inject(BookingService);
  readonly authService = inject(AuthService);

  /** Earliest selectable Romanian minute, refreshed on selection and submission. */
  minimumDateTime = nextRomanianMinute();
  readonly bookingForm = new FormGroup<BookingForm>({
    startDateTime: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    endDateTime: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  listing?: ListingDetails;
  availabilityWindows: AvailabilityWindow[] = [];
  selectedWindowId?: number;
  bookingResult?: BookingResult;
  isLoading = false;
  isSubmitting = false;
  errorMessages: string[] = [];
  bookingErrorMessages: string[] = [];

  get openWindows(): AvailabilityWindow[] {
    return this.availabilityWindows.filter((window) => !window.isBlocked);
  }

  get blockedWindows(): AvailabilityWindow[] {
    return this.availabilityWindows.filter((window) => window.isBlocked);
  }

  get isOwnListing(): boolean {
    return !!this.listing && this.authService.session?.userId === this.listing.ownerId;
  }

  get hasInvalidRange(): boolean {
    const { startDateTime, endDateTime } = this.bookingForm.getRawValue();
    return !!startDateTime && !!endDateTime && startDateTime >= endDateTime;
  }

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    const listingId = rawId ? Number(rawId) : Number.NaN;
    if (!Number.isSafeInteger(listingId) || listingId <= 0) {
      this.errorMessages = ['This listing address is invalid.'];
      return;
    }

    this.loadListing(listingId);
  }

  selectWindow(window: AvailabilityWindow): void {
    if (!this.canSelectWindow(window)) {
      return;
    }

    this.minimumDateTime = nextRomanianMinute();
    this.selectedWindowId = window.availabilityWindowId;
    this.bookingResult = undefined;
    this.bookingErrorMessages = [];
    this.bookingForm.setValue({
      startDateTime: this.windowStart(window),
      endDateTime: window.endDateTime.slice(0, 16)
    });
  }

  submitBooking(): void {
    if (!this.listing || this.isOwnListing || this.isSubmitting) {
      return;
    }

    if (this.bookingForm.invalid || this.hasInvalidRange) {
      this.bookingForm.markAllAsTouched();
      return;
    }

    this.minimumDateTime = nextRomanianMinute();
    const period = this.bookingForm.getRawValue();
    const timeError = bookingTimeError(period.startDateTime, period.endDateTime);
    if (timeError) {
      this.bookingErrorMessages = [timeError];
      return;
    }
    this.isSubmitting = true;
    this.bookingResult = undefined;
    this.bookingErrorMessages = [];

    this.bookingService.create({
      listingId: this.listing.id,
      ...this.bookingForm.getRawValue()
    })
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: (result) => {
          this.bookingResult = result;
        },
        error: (error) => {
          this.bookingErrorMessages = this.getErrorMessages(error, 'Booking request failed. Please review the selected time.');
        }
      });
  }

  private loadListing(listingId: number): void {
    this.isLoading = true;
    this.errorMessages = [];

    forkJoin({
      listing: this.listingService.getById(listingId),
      availability: this.availabilityService.getForListing(listingId, this.minimumDateTime)
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: ({ listing, availability }) => {
          this.listing = listing;
          this.availabilityWindows = availability.items;
        },
        error: (error) => {
          this.errorMessages = this.getErrorMessages(error, 'Listing details could not be loaded. Please try again.');
        }
      });
  }

  /** Whether an open window has a valid future minute available for booking. */
  canSelectWindow(window: AvailabilityWindow): boolean {
    return !window.isBlocked && bookingTimeError(this.windowStart(window), window.endDateTime.slice(0, 16)) === null;
  }

  /** Clips an ongoing window to the next Romanian minute. */
  private windowStart(window: AvailabilityWindow): string {
    const minimum = nextRomanianMinute();
    const start = window.startDateTime.slice(0, 16);
    return start > minimum ? start : minimum;
  }

  private getErrorMessages(error: unknown, fallback: string): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error) && error.error.every((item) => typeof item === 'string')) {
        return error.error;
      }

      if (typeof error.error === 'string' && error.error.trim()) {
        return [error.error];
      }

      if (error.message) {
        return [error.message];
      }
    }

    return [fallback];
  }
}
