import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { OwnerListingItem } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

@Component({
  selector: 'app-owner-listings',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './owner-listings.component.html',
  styleUrl: './owner-workspace.scss'
})
export class OwnerListingsComponent implements OnInit {
  private readonly listingService = inject(ListingService);

  listings: OwnerListingItem[] = [];
  isLoading = false;
  changingId?: number;
  errorMessages: string[] = [];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessages = [];
    this.listingService.getOwnerListings()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (result) => (this.listings = result.items),
        error: (error) => (this.errorMessages = this.getErrors(error, 'Your listings could not be loaded.'))
      });
  }

  setActive(listing: OwnerListingItem, active: boolean): void {
    if (this.changingId) return;
    this.changingId = listing.id;
    this.errorMessages = [];
    const operation = active ? this.listingService.activate(listing.id) : this.listingService.deactivate(listing.id);
    operation.pipe(finalize(() => (this.changingId = undefined))).subscribe({
      next: (result) => (listing.isActive = result.isActive),
      error: (error) => (this.errorMessages = this.getErrors(error, 'The listing status could not be changed.'))
    });
  }

  private getErrors(error: unknown, fallback: string): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error)) return error.error;
      if (typeof error.error === 'string' && error.error.trim()) return [error.error];
      if (error.message) return [error.message];
    }
    return [fallback];
  }
}
