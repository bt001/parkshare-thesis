import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { SearchListingItem } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';
import { ListingMapComponent } from './listing-map.component';

type ListingViewMode = 'list' | 'map';

@Component({
  selector: 'app-listing-list',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, ListingMapComponent],
  templateUrl: './listing-list.component.html',
  styleUrl: './listing-list.component.scss'
})
export class ListingListComponent implements OnInit {
  private readonly listingService = inject(ListingService);

  readonly pageSize = 12;
  listings: SearchListingItem[] = [];
  page = 1;
  totalCount = 0;
  isLoading = false;
  errorMessages: string[] = [];
  viewMode: ListingViewMode = 'list';
  mapListings: SearchListingItem[] = [];
  selectedMapListing: SearchListingItem | null = null;
  isMapLoading = false;
  isMapLoaded = false;
  mapErrorMessages: string[] = [];

  get canGoBack(): boolean {
    return this.page > 1 && !this.isLoading;
  }

  get canGoForward(): boolean {
    return this.page * this.pageSize < this.totalCount && !this.isLoading;
  }

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    if (page < 1 || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessages = [];

    this.listingService.search({ page, pageSize: this.pageSize, onlyActive: true })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (result) => {
          this.listings = result.items;
          this.page = result.page;
          this.totalCount = result.totalCount;
        },
        error: (error) => {
          this.listings = [];
          this.totalCount = 0;
          this.errorMessages = this.getErrorMessages(error);
        }
      });
  }

  setViewMode(viewMode: ListingViewMode): void {
    this.viewMode = viewMode;
    if (viewMode === 'map' && !this.isMapLoaded && !this.isMapLoading) {
      this.loadMapListings();
    }
  }

  retryMap(): void {
    if (!this.isMapLoading) {
      this.loadMapListings();
    }
  }

  selectMapListing(listing: SearchListingItem): void {
    this.selectedMapListing = listing;
  }

  closeMapPreview(): void {
    this.selectedMapListing = null;
  }

  private loadMapListings(): void {
    this.isMapLoading = true;
    this.mapErrorMessages = [];

    this.listingService.searchAll({ onlyActive: true })
      .pipe(finalize(() => (this.isMapLoading = false)))
      .subscribe({
        next: (listings) => {
          this.mapListings = listings;
          this.isMapLoaded = true;
        },
        error: (error) => {
          this.mapListings = [];
          this.isMapLoaded = false;
          this.selectedMapListing = null;
          this.mapErrorMessages = this.getErrorMessages(error);
        }
      });
  }

  private getErrorMessages(error: unknown): string[] {
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

    return ['Listings could not be loaded. Please try again.'];
  }
}
