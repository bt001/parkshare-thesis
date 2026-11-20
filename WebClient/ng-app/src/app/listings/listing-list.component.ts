import { BookingPeriodComponent, BookingPeriodEdit } from '../shared/booking-period.component';
import { bookingTimeError, nextAthensMinute, stayMinutes } from '../services/booking-time';
import { isPlatformBrowser, DatePipe, DecimalPipe, NgTemplateOutlet } from '@angular/common';
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

/** Secondary criteria that can be removed independently from applied results. */
type AppliedFilterKey = 'titleContains' | 'addressContains' | 'minPrice' | 'maxPrice' | 'minRating' | 'amenityIds' | 'onlyAlwaysAvailable';

/** A removable criterion from the applied search, independent of form drafts. */
interface AppliedFilterChip {
  /** Stable identity for rendering and keyboard targets. */
  id: string;
  /** Applied criterion represented by this chip. */
  key: AppliedFilterKey;
  /** Human-readable applied value. */
  label: string;
  /** Individual amenity removed without clearing other selections. */
  amenityId?: number;
}

@Component({
  selector: 'app-listing-list',
  standalone: true,
  imports: [BookingPeriodComponent, DatePipe, NgTemplateOutlet, DecimalPipe, RouterLink, ListingMapComponent, ReactiveFormsModule],
  templateUrl: './listing-list.component.html',
  styleUrls: ['./listing-list.component.scss', './listing-search-bar.scss', './listing-loading.scss', './listing-result-card.scss']
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

  /** Whether filters fit alongside this results layout. */
  get filtersInline(): boolean { return this.layoutWidth >= (this.viewMode === 'map' ? 1440 : 1000); }

  /** Whether draft controls differ from the most recent valid submission. */
  get hasDraftChanges(): boolean {
    return this.appliedDraft !== '' && this.appliedDraft !== this.draftSnapshot();
  }

  /** Shows Apply feedback only for secondary drafts, not ordinary autocomplete typing. */
  get hasSecondaryDraftChanges(): boolean {
    if (!this.appliedDraft) return false;
    const applied = JSON.parse(this.appliedDraft).values;
    const values = this.filterForm.getRawValue();
    return (['radiusKm', 'titleContains', 'addressContains', 'minPrice', 'maxPrice', 'minRating', 'amenityIds', 'onlyAlwaysAvailable'] as const)
      .some(key => JSON.stringify(applied[key]) !== JSON.stringify(values[key]));
  }

  /** Includes geographic intent even when a new location has the same display label. */
  private draftSnapshot(): string {
    return JSON.stringify({ values: this.filterForm.getRawValue(), incompletePeriod: this.incompletePeriod, place: this.selectedPlace, bounds: this.bounds });
  }

  /** Preserves drafts while reconciling a modal during viewport changes. */
  @HostListener('window:resize')
  @HostListener('window:scroll')
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
  /** Earliest selectable Athens minute. */
  minimumDateTime = nextAthensMinute();
  /** Applied criteria carried into detail and restored on return. */
  detailParams: Record<string, string | number | boolean | number[]> = {};
  /** Resets local period drafts even when the restored API values stay blank. */
  periodRevision = 0;
  /** Retains duration-only edits for period validation. */
  private incompletePeriod = false;
  /** Updates a period draft without changing applied search criteria. */
  editPeriod(period: BookingPeriodEdit): void {
    this.incompletePeriod = period.incomplete;
    this.filterForm.patchValue({ startDateTime: period.startDateTime, endDateTime: period.endDateTime });
    this.periodError = null;
    this.schedulePrimarySearch();
  }
  /** Formats only structurally valid saved periods in the compact departure summary. */
  get draftDeparture(): string {
    const { startDateTime, endDateTime } = this.filterForm.getRawValue();
    return stayMinutes(startDateTime, endDateTime) === null ? '' : endDateTime;
  }
  /** Duration of the applied result period, independent of incomplete form drafts. */
  get appliedStayMinutes(): number | null {
    return stayMinutes(this.appliedFilters.startDateTime ?? '', this.appliedFilters.endDateTime ?? '');
  }
  /** Describes applied stay length without rounding fractional hours. */
  get appliedStayDuration(): string {
    const minutes = this.appliedStayMinutes;
    if (minutes === null) return '';
    return [minutes >= 60 ? `${Math.floor(minutes / 60)} h` : '', minutes % 60 ? `${minutes % 60} min` : ''].filter(Boolean).join(' ');
  }
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

  /** Valid secondary state retained while an expired saved period is corrected. */
  private restoredSecondary?: { criteria: Omit<SearchListingsRequest, 'page' | 'pageSize'>; radius: number; draft: string };
  /** Fits the suggestions above or below the field without leaving the visible screen. */
  get placePanelStyle(): Record<string, string> {
    if (!isPlatformBrowser(this.platformId)) return {};
    const field = this.element.nativeElement.querySelector('.place-field')?.getBoundingClientRect();
    if (!field) return {};
    const viewport = window.visualViewport;
    const left = viewport?.offsetLeft ?? 0, top = viewport?.offsetTop ?? 0;
    const width = viewport?.width ?? window.innerWidth, height = viewport?.height ?? window.innerHeight;
    const below = top + height - field.bottom - 20, above = field.top - top - 20;
    const upwards = below < 180 && above > below;
    const panelWidth = Math.min(Math.max(field.width, 280), width - 24);
    return { width: `${panelWidth}px`, left: `${Math.max(left + 12, Math.min(field.left, left + width - panelWidth - 12))}px`,
      top: upwards ? 'auto' : `${Math.max(top + 12, field.bottom + 8)}px`,
      bottom: upwards ? `${window.innerHeight - field.top + 8}px` : 'auto',
      'max-height': `${Math.max(0, Math.min(352, upwards ? above : below))}px` };
  }
  /** Prevents primary text/number inputs from submitting secondary filter drafts on Enter. */
  preventPrimarySubmit(event: Event): void {
    if ((event.target as HTMLElement).matches('input')) event.preventDefault();
  }
  /** Whether the location suggestions panel is open. */
  placePanelOpen = false;
  /** Keyboard-highlighted location candidate. */
  activePlaceIndex = -1;
  /** Debounces name lookup independently of result refresh. */
  private placeTimer?: ReturnType<typeof setTimeout>;
  /** Debounces primary result updates. */
  private primaryTimer?: ReturnType<typeof setTimeout>;
  /** Invalidates late lookup responses even when a provider cannot cancel. */
  private placeGeneration = 0;

  /** Cancels pending lookup work without changing applied location criteria. */
  private cancelPlaceLookup(): void {
    clearTimeout(this.placeTimer);
    this.placeGeneration++;
    this.placeRequest?.unsubscribe();
    this.isPlaceLoading = false;
    this.activePlaceIndex = -1;
  }
  /** Opens suggestions and starts a debounced lookup for unresolved text. */
  openPlaces(): void {
    this.placePanelOpen = true;
    const text = this.filterForm.controls.where.value.trim();
    if (!this.selectedPlace && !this.places.length && !this.isPlaceLoading && text.length >= 2 && text.length <= 200) {
      clearTimeout(this.placeTimer);
      this.placeTimer = setTimeout(() => this.findPlaces(), 350);
    }
  }
  /** Edits destination text without applying unresolved geographic input. */
  typePlace(): void {
    this.editPlace();
    this.placePanelOpen = true;
    const text = this.filterForm.controls.where.value.trim();
    clearTimeout(this.primaryTimer);
    if (!text) this.schedulePrimarySearch();
    else if (text.length >= 2 && text.length <= 200) this.placeTimer = setTimeout(() => this.findPlaces(), 350);
    else if (text.length > 200) this.placeError = 'Use a place name of at most 200 characters.';
  }
  /** Explicitly clears geographic intent and refreshes the valid primary search. */
  clearPlace(): void {
    this.filterForm.controls.where.setValue('');
    this.typePlace();
    this.element.nativeElement.querySelector<HTMLInputElement>('#search-where')?.focus();
  }
  /** Selects a suggestion as a user action, separate from route restoration. */
  selectPlace(place: SearchPlace): void {
    this.choosePlace(place);
    this.placePanelOpen = false;
    this.schedulePrimarySearch();
  }
  /** Supports listbox navigation while focus remains in the destination input. */
  navigatePlaces(event: KeyboardEvent): void {
    if (event.key === 'Escape') { event.preventDefault(); this.dismissPlaces(); return; }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault(); this.placePanelOpen = true;
      if (this.places.length) {
        this.activePlaceIndex = this.activePlaceIndex < 0 ? (event.key === 'ArrowDown' ? 0 : this.places.length - 1)
          : (this.activePlaceIndex + (event.key === 'ArrowDown' ? 1 : this.places.length - 1)) % this.places.length;
        this.element.nativeElement.querySelector<HTMLElement>('#place-option-' + this.activePlaceIndex)?.scrollIntoView({ block: 'nearest' });
      }
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.activePlaceIndex >= 0 && this.places[this.activePlaceIndex]) this.selectPlace(this.places[this.activePlaceIndex]);
      else if (!this.selectedPlace && this.filterForm.controls.where.value.trim().length >= 2) this.findPlaces();
    }
  }
  /** Dismisses suggestions and prevents a pending lookup from reopening them. */
  dismissPlaces(): void { this.placePanelOpen = false; this.cancelPlaceLookup(); }
  /** Closes location suggestions when focus moves elsewhere. */
  placeFocusOut(event: FocusEvent): void {
    if (!(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node | null)) this.dismissPlaces();
  }
  /** Dismisses suggestions on a pointer action outside their field. */
  @HostListener('document:pointerdown', ['$event'])
  outsidePlaces(event: PointerEvent): void {
    if (!(event.target as Element).closest('.place-field')) this.dismissPlaces();
  }
  /** Coalesces primary edits while retaining focused date and duration controls. */
  private schedulePrimarySearch(): void {
    clearTimeout(this.primaryTimer);
    this.primaryTimer = setTimeout(() => this.applyPrimarySearch(), 250);
  }
  /** Compares criteria independently of object insertion order. */
  private criteriaKey(criteria: Omit<SearchListingsRequest, 'page' | 'pageSize'>): string {
    return JSON.stringify(Object.keys(criteria).sort().map(key => [key, criteria[key as keyof typeof criteria]]));
  }
  /** Applies only primary fields, preserving every unsubmitted secondary draft. */
  private applyPrimarySearch(): void {
    const values = this.filterForm.getRawValue();
    this.periodError = this.incompletePeriod ? 'Choose an arrival and a valid length of stay.'
      : values.startDateTime || values.endDateTime ? bookingTimeError(values.startDateTime, values.endDateTime) : null;
    if (this.periodError) return;
    if (values.where.trim() && !this.selectedPlace) {
      this.placeError = 'Select a suggested place or clear Where. Results still show your previous search.';
      return;
    }
    if (this.restorationError && !this.restoredSecondary) return;
    if (this.restoredSecondary) {
      this.appliedRadius = this.restoredSecondary.radius;
      this.appliedDraft = this.restoredSecondary.draft;
    }
    const criteria = { ...(this.restoredSecondary?.criteria ?? this.appliedFilters) };
    for (const key of ['lat', 'lng', 'radiusKm', 'south', 'north', 'west', 'east', 'startDateTime', 'endDateTime'] as const) delete criteria[key];
    Object.assign(criteria, this.bounds ?? {}, this.selectedPlace ? {
      lat: this.selectedPlace.latitude, lng: this.selectedPlace.longitude, radiusKm: this.appliedRadius
    } : {}, values.startDateTime ? { startDateTime: values.startDateTime, endDateTime: values.endDateTime } : {});
    const changed = !!this.restorationError || this.criteriaKey(criteria) !== this.criteriaKey(this.appliedFilters);
    this.appliedFilters = criteria;
    this.hasApplied = true;
    this.searchArea = this.selectedPlace;
    this.placeError = '';
    if (this.appliedDraft) {
      const snapshot = JSON.parse(this.appliedDraft);
      Object.assign(snapshot.values, { where: values.where, startDateTime: values.startDateTime, endDateTime: values.endDateTime });
      Object.assign(snapshot, { incompletePeriod: false, place: this.selectedPlace, bounds: this.bounds });
      this.appliedDraft = JSON.stringify(snapshot);
    }
    if (changed) this.refreshResults();
    else this.syncNavigation();
  }

  /** Candidate places from the latest destination lookup. */
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

  /** Selects device coordinates and schedules an automatic primary refresh. */
  useMyLocation(): void {
    if (this.isLocationLoading) return;
    this.invalidateLocation();
    this.cancelPlaceLookup();
    clearTimeout(this.primaryTimer);
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
        this.selectPlace({ latitude, longitude, label: 'Current location' });
        this.locationStatus = 'Searching around your current location.';
      }, error => fail(error.code === 1
        ? 'Location permission was denied. Allow location in your browser settings to retry.'
        : error.code === 3 ? 'Location request timed out.' : 'Your location is unavailable.'));
    } catch {
      fail('Your location is unavailable.');
    }
  }

  /** Resolves the current name, cancelling superseded suggestion requests. */
  findPlaces(): void {
    this.invalidateLocation();
    const text = this.filterForm.controls.where.value.trim();
    this.cancelPlaceLookup();
    this.placePanelOpen = true;
    const generation = this.placeGeneration;
    this.places = [];
    this.placeError = '';
    if (text.length < 2 || text.length > 200) {
      this.placeError = 'Enter a city, address or landmark between 2 and 200 characters.';
      return;
    }
    this.isPlaceLoading = true;
    this.placeRequest = this.listingService.searchPlaces(text).pipe(finalize(() => { if (generation === this.placeGeneration) this.isPlaceLoading = false; }))
      .subscribe({
        next: result => {
          if (generation !== this.placeGeneration) return;
          this.places = result.items;
          if (!result.items.length) this.placeError = 'No places found. Try a fuller name or use the map.';
        },
        error: () => { if (generation === this.placeGeneration) this.placeError = 'Place lookup is unavailable. Try again or search around the map centre.'; }
      });
  }

  /** Invalidates a selected name and any in-flight lookup when the text changes. */
  editPlace(): void {
    this.invalidateLocation();
    this.cancelPlaceLookup();
    clearTimeout(this.primaryTimer);
    this.selectedPlace = null;
    this.bounds = null;
    this.filterForm.controls.radiusKm.enable();
    this.places = [];
    this.placeError = '';
  }

  /** Selects an explicit geographic candidate without changing applied results yet. */
  choosePlace(place: SearchPlace): void {
    this.invalidateLocation();
    this.cancelPlaceLookup();
    clearTimeout(this.primaryTimer);
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

  /** Displays only applied secondary criteria, including catalog-independent amenities. */
  get appliedFilterChips(): AppliedFilterChip[] {
    if (this.restorationError) return [];
    const criteria = this.appliedFilters;
    const chips: AppliedFilterChip[] = [];
    for (const key of ['titleContains', 'addressContains', 'minPrice', 'maxPrice', 'minRating'] as const) {
      const value = criteria[key];
      if (value === undefined) continue;
      const labels = {
        titleContains: `Title: ${value}`, addressContains: `Address: ${value}`,
        minPrice: `Min: ${value} €/h`, maxPrice: `Max: ${value} €/h`, minRating: `Rating: ${value}+`
      };
      chips.push({ id: key, key, label: labels[key] });
    }
    for (const id of new Set(criteria.amenityIds ?? [])) {
      chips.push({ id: `amenity-${id}`, key: 'amenityIds', amenityId: id,
        label: this.amenities.find(amenity => amenity.id === id)?.name ?? `Amenity #${id}` });
    }
    if (criteria.onlyAlwaysAvailable) chips.push({ id: 'onlyAlwaysAvailable', key: 'onlyAlwaysAvailable', label: 'Always available' });
    return chips;
  }

  /** Applies toolbar sorting without submitting or validating pending form edits. */
  applySort(): void {
    if (this.restorationError) return;
    const sort = this.filterForm.controls.sortBy.value;
    if ((this.appliedFilters.sortBy ?? 'newest') === sort) return;
    const criteria = { ...this.appliedFilters };
    if (sort === 'newest') delete criteria.sortBy;
    else criteria.sortBy = sort;
    this.appliedFilters = criteria;
    this.updateAppliedValue('sortBy', sort);
    this.refreshResults(true);
  }

  /** Removes one applied criterion while retaining unrelated applied values and drafts. */
  removeAppliedFilter(chip: AppliedFilterChip): void {
    if (this.restorationError || !this.appliedFilterChips.some(value => value.id === chip.id)) return;
    const criteria = { ...this.appliedFilters };
    const controls = this.filterForm.controls;
    const key = chip.key;
    if (key === 'amenityIds') {
      const ids = (criteria.amenityIds ?? []).filter(id => id !== chip.amenityId);
      if (ids.length) criteria.amenityIds = ids;
      else delete criteria.amenityIds;
      controls.amenityIds.setValue(controls.amenityIds.value.filter(id => id !== chip.amenityId));
      this.updateAppliedValue(key, ids);
    } else {
      delete criteria[key];
      if (key === 'titleContains' || key === 'addressContains') controls[key].setValue('');
      else if (key === 'onlyAlwaysAvailable') controls[key].setValue(false);
      else controls[key].setValue(null);
      this.updateAppliedValue(key, controls[key].value);
    }
    this.appliedFilters = criteria;
    this.refreshResults(true);
  }

  /** Rebases only the changed applied field so unrelated draft comparisons stay intact. */
  private updateAppliedValue(key: AppliedFilterKey | 'sortBy', value: unknown): void {
    if (!this.appliedDraft) return;
    const snapshot = JSON.parse(this.appliedDraft);
    snapshot.values[key] = value;
    this.appliedDraft = JSON.stringify(snapshot);
  }

  /** Applies valid draft values without retaining blank text or an unchecked toggle. */
  applyFilters(): void {
    this.dismissPlaces();
    clearTimeout(this.primaryTimer);
    this.invalidateLocation();
    if (!this.applyDraft()) {
      this.revealInvalidFilters();
      return;
    }
    this.closeFilters();
    this.refreshResults();
  }

  /** Refreshes applied criteria without dismissing primary controls or stealing focus. */
  private refreshResults(preserveViewport = false): void {
    this.restoredSecondary = undefined;
    this.restorationError = '';
    this.selectedId = null;
    if (!preserveViewport && !this.bounds) this.viewport = null;
    this.invalidateMap();
    this.loadPage(1);
    if (this.viewMode === 'map') this.loadMapListings();
  }

  /** Makes invalid controls reachable when their responsive container is closed. */
  private revealInvalidFilters(): void {
    const host = this.element.nativeElement;
    const dialog = this.filterDialog?.nativeElement;
    const secondaryInvalid = ['titleContains', 'addressContains', 'minPrice', 'maxPrice', 'minRating', 'radiusKm']
      .some(key => this.filterForm.get(key)?.invalid) || this.filterForm.hasError('priceRange');
    if (secondaryInvalid && !this.filtersInline && dialog && !dialog.open) dialog.showModal();
    if (dialog?.matches(':modal')) {
      dialog.querySelector<HTMLElement>('input.ng-invalid, select.ng-invalid')?.focus();
    } else {
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
    this.minimumDateTime = nextAthensMinute();
    this.periodError = this.incompletePeriod ? 'Choose an arrival and a valid length of stay.' : values.startDateTime || values.endDateTime
      ? bookingTimeError(values.startDateTime, values.endDateTime) : null;
    if (this.periodError) return false;
    if (values.where.trim() && !this.selectedPlace) {
      this.placeError = 'Select a suggested place or clear Where to browse all parking.';
      return false;
    }
    this.searchArea = this.selectedPlace;
    this.appliedRadius = values.radiusKm;
    this.appliedFilters = {
      ...this.secondaryCriteria(),
      ...(this.bounds ?? {}),
      ...(this.selectedPlace ? { lat: this.selectedPlace.latitude, lng: this.selectedPlace.longitude, radiusKm: values.radiusKm } : {}),
      ...(values.startDateTime ? { startDateTime: values.startDateTime, endDateTime: values.endDateTime } : {})
    };
    this.appliedDraft = this.draftSnapshot();
    this.hasApplied = true;
    return true;
  }

  /** Reads secondary criteria independently of pending location and period edits. */
  private secondaryCriteria(): Omit<SearchListingsRequest, 'page' | 'pageSize'> {
    const values = this.filterForm.getRawValue();
    return {
      onlyActive: true,
      ...(values.amenityIds.length ? { amenityIds: [...new Set(values.amenityIds)] } : {}),
      ...(values.minRating !== null ? { minRating: values.minRating } : {}),
      ...(values.sortBy !== 'newest' ? { sortBy: values.sortBy } : {}),
      ...(values.titleContains ? { titleContains: values.titleContains } : {}),
      ...(values.addressContains ? { addressContains: values.addressContains } : {}),
      ...(values.minPrice !== null ? { minPrice: values.minPrice } : {}),
      ...(values.maxPrice !== null ? { maxPrice: values.maxPrice } : {}),
      ...(values.onlyAlwaysAvailable ? { onlyAlwaysAvailable: true } : {})
    };
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
    this.placePanelOpen = false;
    this.editPlace();
    this.filterForm.reset();
    this.periodRevision++;
    this.incompletePeriod = false;
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
    this.cancelPlaceLookup();
    clearTimeout(this.primaryTimer);
  }

  readonly pageSize = 12;
  /** Placeholder slots retain the previous page's density without exposing stale results. */
  loadingCards = Array.from({ length: this.pageSize }, (_, index) => index);
  /** Last rendered list height reserved while its replacement is loading. */
  loadingHeight = 0;
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
    clearTimeout(this.primaryTimer);
    this.restoredSecondary = undefined;
    this.placePanelOpen = false;
    this.writtenKey = discoveryParamsKey(params);
    const previous = JSON.stringify(this.appliedFilters);
    const previousPage = this.page;
    const wasApplied = this.hasApplied && !this.restorationError;
    this.editPlace();
    this.filterForm.reset();
    this.periodRevision++;
    this.incompletePeriod = false;
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
      const secondaryValid = !this.filterForm.hasError('priceRange') && ['titleContains', 'addressContains', 'minPrice', 'maxPrice', 'minRating', 'radiusKm']
        .every(key => this.filterForm.get(key)?.valid || this.filterForm.get(key)?.disabled);
      if (!(hasBounds && !validBounds) && !malformed && secondaryValid) {
        this.restoredSecondary = { criteria: this.secondaryCriteria(), radius: this.filterForm.controls.radiusKm.value, draft: this.draftSnapshot() };
      }
      this.restorationError = 'Saved search criteria are invalid or expired. Correct your search or apply valid filters, or reset to browse again.';
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
    const resultHeight = this.element.nativeElement.querySelector('.list-results')?.getBoundingClientRect().height;
    if (resultHeight) this.loadingHeight = resultHeight;
    if (this.listings.length) this.loadingCards = Array.from({ length: this.listings.length }, (_, index) => index);
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
