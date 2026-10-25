import { DecimalPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { OwnerListingItem } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

@Component({
  selector: 'app-owner-listings',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink],
  templateUrl: './owner-listings.component.html',
  styleUrl: './owner-workspace.scss'
})
export class OwnerListingsComponent implements OnInit {
  private readonly listingService = inject(ListingService);

  listings: OwnerListingItem[] = [];
  isLoading = false;
  changingId?: number;
  errorMessages: string[] = [];

  /** Retrieval feedback, separate from publishing failures. */
  loadErrors: string[] = [];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    if (this.isLoading || this.changingId) return;
    this.isLoading = true;
    this.loadErrors = [];
    this.listingService.getOwnerListings()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (result) => (this.listings = result.items),
        error: (error) => (this.loadErrors = this.getErrors(error, 'Your listings could not be loaded.'))
      });
  }

  setActive(listing: OwnerListingItem, active: boolean): void {
    if (this.changingId || this.isLoading || this.loadErrors.length) return;
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
      if (Array.isArray(error.error)) {
        const messages = error.error.filter((message): message is string => typeof message === 'string' && !!message.trim());
        return messages.length ? messages : [fallback];
      }
      if (typeof error.error === 'string' && error.error.trim()) return [error.error];
    }
    return [fallback];
  }
}
