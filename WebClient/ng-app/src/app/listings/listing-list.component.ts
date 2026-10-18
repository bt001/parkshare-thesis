import { bookingTimeError, nextRomanianMinute } from '../services/booking-time';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

import { Amenity, SearchPlace, SearchListingItem, SearchListingsRequest } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';
import { resolveApiUrl } from '../api/api-request';
import { ListingMapComponent } from './listing-map.component';

type ListingViewMode = 'list' | 'map';

@Component({
  selector: 'app-listing-list',
  standalone: true,
  imports: [DecimalPipe, CurrencyPipe, RouterLink, ListingMapComponent, ReactiveFormsModule],
  templateUrl: './listing-list.component.html',
  styleUrl: './listing-list.component.scss'
})
export class ListingListComponent implements OnInit, OnDestroy {
  /** Catalog options and independent retry feedback. */
  amenities: Amenity[] = [];
  /** Safe catalog failure feedback. */
  amenityError = '';
  private amenityRequest?: Subscription;
  /** Broken covers retain a readable fallback. */
  brokenPhotos = new Set<string>();
  /** Uses the configured API origin for public images. */
  readonly imageUrl = resolveApiUrl;
  /** Loads catalog without changing restored selections. */
  loadAmenities(): void {
    this.amenityRequest?.unsubscribe();
    this.amenityError = '';
    this.amenityRequest = this.listingService.getAmenities().subscribe({
      next: result => this.amenities = result.items,
      error: () => this.amenityError = 'Amenities could not be loaded.'
    });
  }
  /** Changes draft selections; Search applies them together. */
  toggleAmenity(id: number): void {
    const control = this.filterForm.controls.amenityIds;
    control.setValue(control.value.includes(id) ? control.value.filter(value => value !== id) : [...control.value, id]);
  }

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  /** Earliest selectable Romanian minute. */
  minimumDateTime = nextRomanianMinute();
  /** Applied criteria carried into detail and restored on return. */
  detailParams: Record<string, string | number | boolean | number[]> = {};
  /** Explains an invalid requested period. */
  periodError: string | null = null;
  private readonly listingService = inject(ListingService);

  /** Candidate places from the latest explicit lookup. */
  places: SearchPlace[] = [];
  /** Chosen geographic centre, distinct from unsubmitted text. */
  selectedPlace: SearchPlace | null = null;
  /** Applied centre shared with the map. */
  searchArea: SearchPlace | null = null;
  /** Applied distance shown by the map. */
  appliedRadius = 5;
  /** Lookup progress and safe error state. */
  isPlaceLoading = false;
  placeError = '';
  private placeRequest?: Subscription;

  /** Resolves names only on explicit submission, cancelling superseded requests. */
  findPlaces(): void {
    const text = this.filterForm.controls.where.value.trim();
    this.placeRequest?.unsubscribe();
    this.places = [];
    this.placeError = '';
    if (text.length < 2 || text.length > 200) {
      this.placeError = 'Enter a city, address or landmark between 2 and 200 characters.';
      return;
    }
    this.isPlaceLoading = true;
    this.placeRequest = this.listingService.searchPlaces(text).pipe(finalize(() => this.isPlaceLoading = false))
      .subscribe({
        next: result => {
          this.places = result.items;
          if (!result.items.length) this.placeError = 'No places found. Try a fuller name or use the map.';
        },
        error: () => this.placeError = 'Place lookup is unavailable. Retry Find place, or browse the map and search around its centre.'
      });
  }

  /** Invalidates a selected name and any in-flight lookup when the text changes. */
  editPlace(): void {
    this.placeRequest?.unsubscribe();
    this.selectedPlace = null;
    this.places = [];
    this.placeError = '';
  }

  /** Selects an explicit geographic candidate without changing applied results yet. */
  choosePlace(place: SearchPlace): void {
    this.placeRequest?.unsubscribe();
    this.selectedPlace = { ...place, label: place.label.slice(0, 200) };
    this.filterForm.controls.where.setValue(this.selectedPlace.label);
    this.places = [];
    this.placeError = '';
  }

  /** Searches a manually chosen map centre while retaining the requested period and filters. */
  searchMapCentre(point: { latitude: number; longitude: number }): void {
    this.choosePlace({ ...point, label: 'Selected map area' });
    this.applyFilters();
  }

  /** Draft filters, applied only after a valid submission. */
  readonly filterForm = new FormGroup({
    amenityIds: new FormControl<number[]>([], { nonNullable: true }),
    minRating: new FormControl<number | null>(null, [Validators.min(1), Validators.max(5)]),
    sortBy: new FormControl<NonNullable<SearchListingsRequest['sortBy']>>('newest', { nonNullable: true }),
    where: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
    radiusKm: new FormControl(5, { nonNullable: true, validators: [Validators.min(0.1), Validators.max(100)] }),
    startDateTime: new FormControl('', { nonNullable: true }),
    endDateTime: new FormControl('', { nonNullable: true }),
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
    return Object.keys(this.appliedFilters).some(key => key !== 'onlyActive' && key !== 'sortBy');
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
    this.minimumDateTime = nextRomanianMinute();
    this.periodError = values.startDateTime || values.endDateTime
      ? bookingTimeError(values.startDateTime, values.endDateTime) : null;
    if (this.periodError) return;
    if (values.where.trim() && !this.selectedPlace) {
      this.placeError = 'Use Find place and select a result, or clear Where to browse all parking.';
      return;
    }
    this.searchArea = this.selectedPlace;
    this.appliedRadius = values.radiusKm;
    this.appliedFilters = {
      onlyActive: true,
      ...(values.amenityIds.length ? { amenityIds: [...new Set(values.amenityIds)] } : {}),
      ...(values.minRating !== null ? { minRating: values.minRating } : {}),
      ...(values.sortBy !== 'newest' ? { sortBy: values.sortBy } : {}),
      ...(this.selectedPlace ? { lat: this.selectedPlace.latitude, lng: this.selectedPlace.longitude, radiusKm: values.radiusKm } : {}),
      ...(values.startDateTime ? { startDateTime: values.startDateTime, endDateTime: values.endDateTime } : {}),
      ...(values.titleContains ? { titleContains: values.titleContains } : {}),
      ...(values.addressContains ? { addressContains: values.addressContains } : {}),
      ...(values.minPrice !== null ? { minPrice: values.minPrice } : {}),
      ...(values.maxPrice !== null ? { maxPrice: values.maxPrice } : {}),
      ...(values.onlyAlwaysAvailable ? { onlyAlwaysAvailable: true } : {})
    };
    const { onlyActive, ...criteria } = this.appliedFilters;
    this.detailParams = { ...criteria, ...(this.selectedPlace ? { place: this.selectedPlace.label } : {}) };
    void this.router.navigate([], { relativeTo: this.route, queryParams: this.detailParams, replaceUrl: true });
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
    this.editPlace();
    this.filterForm.reset();
    this.applyFilters();
  }

  /** Cancels requests when leaving discovery. */
  ngOnDestroy(): void {
    this.listRequest?.unsubscribe();
    this.mapRequest?.unsubscribe();
    this.placeRequest?.unsubscribe();
    this.amenityRequest?.unsubscribe();
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
    this.loadAmenities();
    const params = this.route.snapshot.queryParamMap;
    const lat = Number(params.get('lat'));
    const lng = Number(params.get('lng'));
    const radius = Number(params.get('radiusKm'));
    if (params.has('lat') && params.has('lng') && Number.isFinite(lat) && Number.isFinite(lng)
        && Math.abs(lat) <= 90 && Math.abs(lng) <= 180 && radius >= 0.1 && radius <= 100) {
      this.choosePlace({ label: params.get('place')?.slice(0, 200) || 'Selected map area', latitude: lat, longitude: lng });
      this.filterForm.controls.radiusKm.setValue(radius);
    }
    for (const key of ['titleContains', 'addressContains', 'startDateTime', 'endDateTime'] as const) {
      this.filterForm.controls[key].setValue(params.get(key) ?? '');
    }
    for (const key of ['minPrice', 'maxPrice', 'minRating'] as const) {
      const value = params.get(key);
      this.filterForm.controls[key].setValue(value !== null && Number.isFinite(Number(value)) ? Number(value) : null);
    }
    this.filterForm.controls.amenityIds.setValue([...new Set(params.getAll('amenityIds').map(Number).filter(id => Number.isSafeInteger(id) && id > 0))]);
    const sort = params.get('sortBy');
    this.filterForm.controls.sortBy.setValue(sort === 'priceAsc' || sort === 'priceDesc' || sort === 'ratingDesc' ? sort : 'newest');
    this.filterForm.controls.onlyAlwaysAvailable.setValue(params.get('onlyAlwaysAvailable') === 'true');
    this.applyFilters();
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
