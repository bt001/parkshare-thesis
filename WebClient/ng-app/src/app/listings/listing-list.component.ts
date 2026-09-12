import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

import { SearchListingItem, SearchListingsRequest } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';
import { ListingMapComponent } from './listing-map.component';

type ListingViewMode = 'list' | 'map';

@Component({
  selector: 'app-listing-list',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, ListingMapComponent, ReactiveFormsModule],
  templateUrl: './listing-list.component.html',
  styleUrl: './listing-list.component.scss'
})
export class ListingListComponent implements OnInit, OnDestroy {
  private readonly listingService = inject(ListingService);

  /** Draft filters, applied only after a valid submission. */
  readonly filterForm = new FormGroup({
    titleContains: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(120)] }),
    addressContains: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
    minPrice: new FormControl<number | null>(null, Validators.min(0)),
    maxPrice: new FormControl<number | null>(null, Validators.min(0)),
    onlyAlwaysAvailable: new FormControl(false, { nonNullable: true })
  }, { validators: (form) => {
    const { minPrice, maxPrice } = form.value;
    return minPrice != null && maxPrice != null && minPrice > maxPrice ? { priceRange: true } : null;
  } });

  /** The immutable filter snapshot shared by list and map requests. */
  private appliedFilters: Omit<SearchListingsRequest, 'page' | 'pageSize'> = { onlyActive: true };
  /** Current list request, cancelled before its replacement starts. */
  private listRequest?: Subscription;
  /** Current map aggregation, cancelled when filters change. */
  private mapRequest?: Subscription;

  /** Whether the displayed results have any discovery filters applied. */
  get hasFilters(): boolean {
    return Object.keys(this.appliedFilters).length > 1;
  }

  /** Applies valid draft values without retaining blank text or an unchecked toggle. */
  applyFilters(): void {
    this.filterForm.patchValue({
      titleContains: this.filterForm.controls.titleContains.value.trim(),
      addressContains: this.filterForm.controls.addressContains.value.trim()
    });
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return;
    }

    const values = this.filterForm.getRawValue();
    this.appliedFilters = {
      onlyActive: true,
      ...(values.titleContains ? { titleContains: values.titleContains } : {}),
      ...(values.addressContains ? { addressContains: values.addressContains } : {}),
      ...(values.minPrice !== null ? { minPrice: values.minPrice } : {}),
      ...(values.maxPrice !== null ? { maxPrice: values.maxPrice } : {}),
      ...(values.onlyAlwaysAvailable ? { onlyAlwaysAvailable: true } : {})
    };
    this.mapRequest?.unsubscribe();
    this.mapListings = [];
    this.isMapLoaded = false;
    this.mapErrorMessages = [];
    this.selectedMapListing = null;
    this.page = 1;
    this.loadPage(1);
    if (this.viewMode === 'map') this.loadMapListings();
  }

  /** Clears the draft and immediately restores active discovery results. */
  resetFilters(): void {
    this.filterForm.reset();
    this.applyFilters();
  }

  /** Cancels requests when leaving discovery. */
  ngOnDestroy(): void {
    this.listRequest?.unsubscribe();
    this.mapRequest?.unsubscribe();
  }

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
    if (page < 1) {
      return;
    }

    this.listRequest?.unsubscribe();
    this.isLoading = true;
    this.listings = [];
    this.totalCount = 0;
    this.errorMessages = [];

    this.listRequest = this.listingService.search({ ...this.appliedFilters, page, pageSize: this.pageSize })
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
    this.mapRequest?.unsubscribe();
    this.isMapLoading = true;
    this.mapErrorMessages = [];

    this.mapRequest = this.listingService.searchAll(this.appliedFilters)
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
