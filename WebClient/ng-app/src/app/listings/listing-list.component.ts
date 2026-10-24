import { bookingTimeError, nextRomanianMinute } from '../services/booking-time';
import { isPlatformBrowser, DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, HostListener, OnDestroy, OnInit, PLATFORM_ID, ViewChild, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, convertToParamMap, ParamMap, Router, RouterLink } from '@angular/router';
import { finalize, skip, Subscription, switchMap, of } from 'rxjs';

import { SearchBounds, Amenity, SearchPlace, SearchListingItem, SearchListingsRequest } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';
import { resolveApiUrl } from '../api/api-request';
import { BrowserLocationService } from '../services/browser-location.service';
import { ListingMapComponent } from './listing-map.component';

import { MapViewport, readDiscoveryNavigation, writeDiscoveryNavigation, discoveryParamsKey } from './discovery-navigation';

type ListingViewMode = 'list' | 'map';

@Component({
  selector: 'app-listing-list',
  standalone: true,
  imports: [NgTemplateOutlet, DecimalPipe, RouterLink, ListingMapComponent, ReactiveFormsModule],
  templateUrl: './listing-list.component.html',
  styleUrl: './listing-list.component.scss'
})
export class ListingListComponent implements OnInit, OnDestroy {
  /** Single filter panel, presented inline only when there is sufficient room. */
  @ViewChild('filterDialog') private filterDialog?: ElementRef<HTMLDialogElement>;
  /** Trigger receiving focus after modal dismissal. */
  @ViewChild('filterTrigger') private filterTrigger?: ElementRef<HTMLButtonElement>;
  /** Browser-only layout boundary; protected routes remain client-rendered. */
  private readonly platformId = inject(PLATFORM_ID);
  /** Current viewport width used to choose sidebar or modal presentation. */
  private layoutWidth = isPlatformBrowser(this.platformId) ? window.innerWidth : 0;
  /** Last applied form snapshot, separate from URL and API criteria. */
  private appliedDraft = '';

  /** Keeps the primary search compact on small screens until explicitly expanded. */
  get searchExpanded(): boolean { return this.layoutWidth >= 768; }

  /** Whether filters fit alongside this results layout. */
  get filtersInline(): boolean { return this.layoutWidth >= (this.viewMode === 'map' ? 1440 : 1000); }

  /** Whether draft controls differ from the most recent valid submission. */
  get hasDraftChanges(): boolean {
    return this.appliedDraft !== '' && this.appliedDraft !== this.draftSnapshot();
  }

  /** Includes geographic intent even when a new location has the same display label. */
  private draftSnapshot(): string {
    return JSON.stringify({ values: this.filterForm.getRawValue(), place: this.selectedPlace, bounds: this.bounds });
  }

  /** Preserves drafts while reconciling a modal during viewport changes. */
  @HostListener('window:resize')
  updateFilterLayout(): void {
    const dialog = this.filterDialog?.nativeElement;
    this.layoutWidth = window.innerWidth;
    if (this.filtersInline && dialog?.matches(':modal')) dialog.close();
  }

  /** Opens the secondary filters with native focus containment. */
  openFilters(): void { this.filterDialog?.nativeElement.showModal(); }

  /** Cycles Tab within the modal without moving focus into browser chrome. */
  containFilterFocus(event: KeyboardEvent): void {
    const dialog = this.filterDialog?.nativeElement;
    if (event.key !== 'Tab' || !dialog?.matches(':modal')) return;
    const controls = Array.from(dialog.querySelectorAll<HTMLElement>('button, input, select, textarea, a[href], [tabindex]'))
      .filter(control => control.tabIndex >= 0 && !control.matches(':disabled') && control.getClientRects().length > 0);
    const first = controls[0];
    const last = controls[controls.length - 1];
    if (event.shiftKey && dialog.ownerDocument.activeElement === first) {
      event.preventDefault();
      last?.focus();
    } else if (!event.shiftKey && dialog.ownerDocument.activeElement === last) {
      event.preventDefault();
      first?.focus();
    }
  }

  /** Dismisses without applying drafts and restores the invoking control. */
  closeFilters(): void {
    if (this.filterDialog?.nativeElement.matches(':modal')) {
      this.filterDialog.nativeElement.close();
      this.filterTrigger?.nativeElement.focus();
    }
  }

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

  /** Saved camera shared with the map. */
  viewport: MapViewport | null = null;
  /** Invalid restored criteria must be corrected before loading results. */
  restorationError = '';
  /** Selected identity survives transient map failures. */
  private selectedId: number | null = null;
  /** Subscription for navigation while this route remains mounted. */
  private routeRequest?: Subscription;
  /** Canonical state last written by this component. */
  private writtenKey = '';
  /** Whether any valid criteria have been applied. */
  private hasApplied = false;

  /** Candidate places from the latest explicit lookup. */
  places: SearchPlace[] = [];
  /** Chosen geographic centre, distinct from unsubmitted text. */
  selectedPlace: SearchPlace | null = null;
  /** Applied centre shared with the map. */
  searchArea: SearchPlace | null = null;
  /** Draft bounds retained until a place or Reset replaces them. */
  bounds: SearchBounds | null = null;
  /** Applied distance shown by the map. */
  appliedRadius = 5;
  /** Lookup progress and safe error state. */
  isPlaceLoading = false;
  placeError = '';
  private placeRequest?: Subscription;

  /** Browser boundary for explicit device-location requests. */
  private readonly browserLocation = inject(BrowserLocationService);
  /** Whether the current location request is still pending. */
  isLocationLoading = false;
  /** Accessible progress and draft-selection feedback. */
  locationStatus = '';
  /** Actionable device-location failure feedback. */
  locationError = '';
  /** Invalidates callbacks from requests that cannot be physically cancelled. */
  private locationGeneration = 0;
  /** Bounds waiting even when a permission prompt remains unanswered. */
  private locationDeadline?: ReturnType<typeof setTimeout>;

  /** Discards pending callbacks and clears transient location feedback. */
  private invalidateLocation(): void {
    this.locationGeneration++;
    clearTimeout(this.locationDeadline);
    this.locationDeadline = undefined;
    this.isLocationLoading = false;
    this.locationStatus = '';
    this.locationError = '';
  }

  /** Selects device coordinates as a draft; Search remains the apply boundary. */
  useMyLocation(): void {
    if (this.isLocationLoading) return;
    this.invalidateLocation();
    this.placeRequest?.unsubscribe();
    this.places = [];
    this.placeError = '';
    const fallback = ' Try again, find a place, or search around the map centre.';
    if (this.browserLocation.unavailableReason) {
      this.locationError = this.browserLocation.unavailableReason + fallback;
      return;
    }
    const generation = this.locationGeneration;
    const fail = (message: string): void => {
      if (generation !== this.locationGeneration) return;
      this.invalidateLocation();
      this.locationError = message + fallback;
    };
    this.isLocationLoading = true;
    this.locationStatus = 'Getting location…';
    this.locationDeadline = setTimeout(() => fail('Location request timed out.'), 15_000);
    try {
      this.browserLocation.locate(position => {
        if (generation !== this.locationGeneration) return;
        const { latitude, longitude } = position.coords;
        if (!Number.isFinite(latitude) || !Number.isFinite(longitude)
            || Math.abs(latitude) > 90 || Math.abs(longitude) > 180) {
          fail('Your location is unavailable.');
          return;
        }
        this.choosePlace({ latitude, longitude, label: 'Current location' });
        this.locationStatus = 'Location selected. Search to apply your distance and filters.';
      }, error => fail(error.code === 1
        ? 'Location permission was denied. Allow location in your browser settings to retry.'
        : error.code === 3 ? 'Location request timed out.' : 'Your location is unavailable.'));
    } catch {
      fail('Your location is unavailable.');
    }
  }

  /** Resolves names only on explicit submission, cancelling superseded requests. */
  findPlaces(): void {
    this.invalidateLocation();
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
    this.invalidateLocation();
    this.placeRequest?.unsubscribe();
    this.selectedPlace = null;
    this.bounds = null;
    this.filterForm.controls.radiusKm.enable();
    this.places = [];
    this.placeError = '';
  }

  /** Selects an explicit geographic candidate without changing applied results yet. */
  choosePlace(place: SearchPlace): void {
    this.invalidateLocation();
    this.placeRequest?.unsubscribe();
    this.bounds = null;
    this.filterForm.controls.radiusKm.enable();
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

  /** Applies actual visible bounds with current filters while retaining the camera. */
  searchMapBounds(bounds: SearchBounds): void {
    this.editPlace();
    this.filterForm.controls.where.setValue('');
    this.bounds = bounds;
    this.filterForm.controls.radiusKm.disable();
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
    this.invalidateLocation();
    if (!this.applyDraft()) {
      this.revealInvalidFilters();
      return;
    }
    this.closeFilters();
    this.restorationError = '';
    this.selectedId = null;
    if (!this.bounds) this.viewport = null;
    this.invalidateMap();
    this.loadPage(1);
    if (this.viewMode === 'map') this.loadMapListings();
  }

  /** Makes invalid controls reachable when their responsive container is closed. */
  private revealInvalidFilters(): void {
    const host = this.element.nativeElement;
    const dialog = this.filterDialog?.nativeElement;
    const secondaryInvalid = ['titleContains', 'addressContains', 'minPrice', 'maxPrice', 'minRating']
      .some(key => this.filterForm.get(key)?.invalid) || this.filterForm.hasError('priceRange');
    if (secondaryInvalid && !this.filtersInline && dialog && !dialog.open) dialog.showModal();
    if (dialog?.matches(':modal')) {
      dialog.querySelector<HTMLElement>('input.ng-invalid, select.ng-invalid')?.focus();
    } else {
      const primary = host.querySelector<HTMLDetailsElement>('.primary-search');
      if (primary && !secondaryInvalid) primary.open = true;
      host.querySelector<HTMLElement>('input.ng-invalid, select.ng-invalid')?.focus();
    }
  }

  /** Validates the draft and captures criteria without resetting navigation. */
  private applyDraft(): boolean {
    this.filterForm.patchValue({
      titleContains: this.filterForm.controls.titleContains.value.trim(),
      addressContains: this.filterForm.controls.addressContains.value.trim()
    });
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return false;
    }

    const values = this.filterForm.getRawValue();
    this.minimumDateTime = nextRomanianMinute();
    this.periodError = values.startDateTime || values.endDateTime
      ? bookingTimeError(values.startDateTime, values.endDateTime) : null;
    if (this.periodError) return false;
    if (values.where.trim() && !this.selectedPlace) {
      this.placeError = 'Use Find place and select a result, or clear Where to browse all parking.';
      return false;
    }
    this.searchArea = this.selectedPlace;
    this.appliedRadius = values.radiusKm;
    this.appliedFilters = {
      onlyActive: true,
      ...(this.bounds ?? {}),
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
    this.appliedDraft = this.draftSnapshot();
    this.hasApplied = true;
    return true;
  }

  /** Discards results tied to previous search criteria. */
  private invalidateMap(): void {
    this.mapRequest?.unsubscribe();
    this.mapListings = [];
    this.isMapLoaded = false;
    this.mapErrorMessages = [];
    this.selectedMapListing = null;
  }

  /** Keeps detail links and the current history entry synchronized. */
  private syncNavigation(): void {
    if (this.restorationError) return;
    const { onlyActive, ...criteria } = this.appliedFilters;
    this.detailParams = { ...criteria, ...(this.searchArea ? { place: this.searchArea.label } : {}),
      ...writeDiscoveryNavigation({ view: this.viewMode, page: this.page, selectedId: this.selectedId, viewport: this.viewport }) };
    this.writtenKey = discoveryParamsKey(convertToParamMap(this.detailParams));
    if (this.writtenKey !== discoveryParamsKey(this.route.snapshot.queryParamMap)) {
      void this.router.navigate([], { relativeTo: this.route, queryParams: this.detailParams, replaceUrl: true });
    }
  }

  /** Saves settled movement without changing the applied search area. */
  saveViewport(viewport: MapViewport): void {
    this.viewport = viewport;
    this.syncNavigation();
  }

  /** Clears the draft and immediately restores active discovery results. */
  resetFilters(): void {
    this.editPlace();
    this.filterForm.reset();
    this.applyFilters();
  }

  /** Cancels requests when leaving discovery. */
  ngOnDestroy(): void {
    this.invalidateLocation();
    this.listRequest?.unsubscribe();
    this.mapRequest?.unsubscribe();
    this.placeRequest?.unsubscribe();
    this.amenityRequest?.unsubscribe();
    this.routeRequest?.unsubscribe();
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
    this.routeRequest?.unsubscribe();
    this.restoreNavigation(this.route.snapshot.queryParamMap);
    this.routeRequest = this.route.queryParamMap.pipe(skip(1)).subscribe(params => {
      if (discoveryParamsKey(params) !== this.writtenKey) this.restoreNavigation(params);
    });
  }

  /** Restores route state without submitting a new search or discarding pagination. */
  private restoreNavigation(params: ParamMap): void {
    this.writtenKey = discoveryParamsKey(params);
    const previous = JSON.stringify(this.appliedFilters);
    const previousPage = this.page;
    const wasApplied = this.hasApplied && !this.restorationError;
    this.editPlace();
    this.filterForm.reset();
    this.restorationError = '';
    const navigation = readDiscoveryNavigation(params);
    this.viewMode = navigation.view;
    this.page = navigation.page;
    this.selectedId = navigation.selectedId;
    this.viewport = navigation.viewport;
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
    const boundsKeys = ['south', 'north', 'west', 'east'] as const;
    const hasBounds = boundsKeys.some(key => params.has(key));
    const validBounds = boundsKeys.every(key => !!params.get(key)?.trim() && Number.isFinite(Number(params.get(key))))
      && Number(params.get('south')) >= -90 && Number(params.get('north')) <= 90
      && Number(params.get('south')) < Number(params.get('north'))
      && Math.abs(Number(params.get('west'))) <= 180 && Math.abs(Number(params.get('east'))) <= 180
      && Number(params.get('west')) !== Number(params.get('east'))
      && !(Number(params.get('west')) === 180 && Number(params.get('east')) === -180)
      && !['lat', 'lng', 'radiusKm'].some(key => params.has(key));
    if (hasBounds && validBounds) this.bounds = {
      south: Number(params.get('south')), north: Number(params.get('north')),
      west: Number(params.get('west')), east: Number(params.get('east'))
    };
    if (this.bounds) this.filterForm.controls.radiusKm.disable();
    const numericKeys = ['minPrice', 'maxPrice', 'minRating'];
    const malformed = numericKeys.some(key => params.has(key) && (!params.get(key)?.trim() || !Number.isFinite(Number(params.get(key)))))
      || (['lat', 'lng', 'radiusKm'].some(key => params.has(key)) && (!this.selectedPlace || ['lat', 'lng', 'radiusKm'].some(key => !params.get(key)?.trim())))
      || params.getAll('amenityIds').some(value => !Number.isSafeInteger(Number(value)) || Number(value) <= 0)
      || (params.has('sortBy') && !['newest', 'priceAsc', 'priceDesc', 'ratingDesc'].includes(params.get('sortBy')!))
      || (params.has('onlyAlwaysAvailable') && !['true', 'false'].includes(params.get('onlyAlwaysAvailable')!));
    if ((hasBounds && !validBounds) || malformed || !this.applyDraft()) {
      this.restorationError = 'Saved search criteria are invalid or expired. Correct the filters and Search, or Reset to browse again.';
      this.filterForm.markAllAsTouched();
      this.listRequest?.unsubscribe();
      this.listings = [];
      this.totalCount = 0;
      this.invalidateMap();
      return;
    }
    const changed = !wasApplied || previous !== JSON.stringify(this.appliedFilters);
    if (changed) this.invalidateMap();
    if (changed || previousPage !== this.page) this.loadPage(this.page);
    if (this.isMapLoaded) this.resolveSelection();
    if (this.viewMode === 'map' && !this.isMapLoaded && !this.isMapLoading) this.loadMapListings();
    this.syncNavigation();
  }

  loadPage(page: number): void {
    if (this.restorationError || !Number.isInteger(page) || page < 1) {
      return;
    }

    this.listRequest?.unsubscribe();
    this.page = page;
    this.syncNavigation();
    this.isLoading = true;
    this.listings = [];
    this.totalCount = 0;
    this.errorMessages = [];

    this.listRequest = this.listingService.search({ ...this.appliedFilters, page, pageSize: this.pageSize })
      .pipe(switchMap(result => {
        const lastPage = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
        if (page <= lastPage) return of(result);
        this.page = lastPage;
        this.syncNavigation();
        return result.totalCount === 0 ? of({ ...result, page: 1 })
          : this.listingService.search({ ...this.appliedFilters, page: lastPage, pageSize: this.pageSize });
      }), finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (result) => {
          this.listings = result.items;
          this.page = result.page;
          this.totalCount = result.totalCount;
          this.syncNavigation();
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
    this.syncNavigation();
    if (!this.restorationError && viewMode === 'map' && !this.isMapLoaded && !this.isMapLoading) {
      this.loadMapListings();
    }
  }

  retryMap(): void {
    if (!this.restorationError && !this.isMapLoading) {
      this.loadMapListings();
    }
  }

  /** Selects the matching card without moving keyboard focus from the marker. */
  selectMapListing(listing: SearchListingItem): void {
    this.selectedMapListing = listing;
    this.selectedId = listing.id;
    this.syncNavigation();
    this.element.nativeElement.querySelector<HTMLElement>(`.map-result-list [data-card-id="${listing.id}"]`)
      ?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
  }

  /** Opens the connected map and highlights the requested result. */
  showOnMap(listing: SearchListingItem): void {
    this.selectedMapListing = listing;
    this.selectedId = listing.id;
    this.syncNavigation();
    this.setViewMode('map');
  }

  /** Limits linked-card scrolling to this discovery view. */
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  closeMapPreview(): void {
    this.selectedMapListing = null;
    this.selectedId = null;
    this.syncNavigation();
  }

  /** Resolves only successful results, clearing unavailable selections. */
  private resolveSelection(): void {
    this.selectedMapListing = this.mapListings.find(item => item.id === this.selectedId) ?? null;
    this.selectedId = this.selectedMapListing?.id ?? null;
    this.syncNavigation();
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
          this.resolveSelection();
        },
        error: (error) => {
          this.mapListings = [];
          this.isMapLoaded = false;
          this.selectedMapListing = null;
          this.mapErrorMessages = this.getErrorMessages(error);
        }
      });
  }

  /** Keeps failures visible and retryable even when the server sends an empty error list. */
  private getErrorMessages(error: unknown): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error) && error.error.length > 0 && error.error.every((item) => typeof item === 'string' && item.trim())) {
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
