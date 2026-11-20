import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, ParamMap } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { SearchListingItem, SearchListingsResult } from '../api/listings/listing.models';

import { ListingService } from '../api/listings/listing.service';
import { BrowserLocationService } from '../services/browser-location.service';
import { ListingListComponent } from './listing-list.component';

describe('ListingListComponent', () => {
  let component: ListingListComponent;
  let fixture: ComponentFixture<ListingListComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['search', 'searchAll', 'searchPlaces', 'getAmenities']);

  const location = { unavailableReason: '', locate: jasmine.createSpy('locate') };

  beforeEach(async () => {
    location.unavailableReason = '';
    location.locate.calls.reset();
    location.locate.and.stub();
    listingService.getAmenities.and.returnValue(of({ items: [] }));
    listingService.searchPlaces.calls.reset();
    listingService.search.calls.reset();
    listingService.searchAll.calls.reset();
    listingService.search.and.returnValue(of({
      page: 1,
      pageSize: 12,
      totalCount: 13,
      items: [{
        id: 7,
        title: 'Central parking space',
        address: '10 Main Street',
        latitude: 44.43,
        longitude: 26.1,
        pricePerHour: 12,
        isActive: true,
        isAlwaysAvailable: false,
        createdAt: '2026-07-13T08:00:00'
      }]
    }));
    listingService.searchAll.and.returnValue(of([{
      id: 7,
      title: 'Central parking space',
      address: '10 Main Street',
      latitude: 44.43,
      longitude: 26.1,
      pricePerHour: 12,
      isActive: true,
      isAlwaysAvailable: false,
      createdAt: '2026-07-13T08:00:00'
    }]));

    await TestBed.configureTestingModule({
      imports: [ListingListComponent],
      providers: [
        provideRouter([]),
        { provide: ListingService, useValue: listingService },
        { provide: BrowserLocationService, useValue: location }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ListingListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('reserves initial loading cards with an accessible status and no result actions', () => {
    fixture.destroy();
    listingService.search.and.returnValue(new Subject<SearchListingsResult>());
    fixture = TestBed.createComponent(ListingListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelectorAll('.loading-card').length).toBe(12);
    expect(host.querySelector('.list-results')?.getAttribute('aria-busy')).toBe('true');
    expect(host.querySelector('.loading-status')?.textContent).toContain('Loading available parking');
    expect(host.querySelector('.listing-grid')?.getAttribute('aria-hidden')).toBe('true');
    expect(host.querySelector('.list-results a, .list-results button')).toBeNull();
    expect(host.textContent).not.toContain('No active listings yet');
  });

  it('replaces old results during paging and preserves their measured height', () => {
    const pending = new Subject<SearchListingsResult>();
    const results = fixture.nativeElement.querySelector('.list-results') as HTMLElement;
    spyOn(results, 'getBoundingClientRect').and.returnValue({ height: 920 } as DOMRect);
    listingService.search.and.returnValue(pending);
    component.loadPage(2); fixture.detectChanges();
    expect(results.style.minHeight).toBe('920px');
    expect(results.querySelectorAll('.loading-card').length).toBe(1);
    expect(results.querySelector('[data-card-id]')).toBeNull();
    expect(results.querySelector('a, button')).toBeNull();
    pending.next({ page: 2, pageSize: 12, totalCount: 13, items: [] }); pending.complete();
    fixture.detectChanges();
    expect(results.style.minHeight).toBe('');
    expect(results.getAttribute('aria-busy')).toBe('false');
    expect(results.querySelector('.loading-card')).toBeNull();
  });

  it('shows only the latest response after rapid searches, including late old failures', () => {
    const old = new Subject<SearchListingsResult>();
    const latest = new Subject<SearchListingsResult>();
    const item = component.listings[0];
    listingService.search.and.returnValues(old, latest);
    component.filterForm.controls.sortBy.setValue('priceAsc'); component.applySort();
    component.filterForm.controls.sortBy.setValue('ratingDesc'); component.applySort();
    expect(old.observed).toBeFalse();
    old.error(new Error('Obsolete failure'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.loading-card')).not.toBeNull();
    expect(component.errorMessages).toEqual([]);
    latest.next({ page: 1, pageSize: 12, totalCount: 1, items: [{ ...item, title: 'Latest match' }] }); latest.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-card-id] h2').textContent).toBe('Latest match');
    expect(fixture.nativeElement.querySelector('.loading-card')).toBeNull();
  });

  it('distinguishes failed loading, retrying and genuine empty results', () => {
    const failed = new Subject<SearchListingsResult>();
    const retry = new Subject<SearchListingsResult>();
    listingService.search.and.returnValues(failed, retry);
    component.loadPage(1); failed.error(new HttpErrorResponse({ status: 503, error: ['Search unavailable'] }));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.list-results [role="alert"]').textContent).toContain('Search unavailable');
    expect(fixture.nativeElement.querySelector('.loading-card')).toBeNull();
    fixture.nativeElement.querySelector('.list-results button').click(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.loading-card')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.list-results [role="alert"]')).toBeNull();
    retry.next({ page: 1, pageSize: 12, totalCount: 0, items: [] }); retry.complete(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No active listings yet');
    expect(fixture.nativeElement.querySelector('.loading-card')).toBeNull();
    expect(fixture.nativeElement.querySelector('.loading-status').textContent.trim()).toBe('');
  });

  it('reserves the map area and hides old cards and preview during a delayed replacement', () => {
    component.setViewMode('map'); fixture.detectChanges();
    const pending = new Subject<SearchListingItem[]>();
    listingService.searchAll.and.returnValue(pending);
    component.filterForm.controls.sortBy.setValue('priceAsc'); component.applySort(); fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.loading-map')).not.toBeNull();
    expect(host.querySelector('.loading-status')?.textContent).toContain('Loading all parking locations');
    expect(host.querySelector('.loading-map')?.parentElement?.getAttribute('aria-hidden')).toBe('true');
    expect(host.querySelector('[data-card-id], app-listing-map, .map-preview')).toBeNull();
    pending.error(new Error('offline')); fixture.detectChanges();
    expect(host.querySelector('.loading-map')).toBeNull();
    expect(host.querySelector('[role="alert"]')?.textContent).toContain('Listings could not be loaded');
  });

  it('sorts immediately using applied criteria while preserving invalid secondary and primary drafts', () => {
    component.filterForm.patchValue({ minPrice: 4, maxPrice: 20, amenityIds: [1] }); component.applyFilters();
    component.loadPage(2);
    component.filterForm.patchValue({ minPrice: 30, radiusKm: -1, titleContains: 'Draft', where: 'Unresolved', startDateTime: 'bad' });
    component.filterForm.controls.minPrice.markAsTouched();
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('#filter-sort') as HTMLSelectElement;
    select.value = 'priceAsc'; select.dispatchEvent(new Event('change'));
    expect(listingService.search.calls.mostRecent().args[0]).toEqual({ onlyActive: true, minPrice: 4, maxPrice: 20, amenityIds: [1], sortBy: 'priceAsc', page: 1, pageSize: 12 });
    expect(component.detailParams['sortBy']).toBe('priceAsc');
    expect(component.detailParams['page']).toBeUndefined();
    expect(component.filterForm.controls.minPrice.value).toBe(30);
    expect(component.filterForm.controls.minPrice.touched).toBeTrue();
    expect(component.filterForm.controls.where.value).toBe('Unresolved');
    expect(component.filterForm.controls.startDateTime.value).toBe('bad');
    expect(component.filterForm.controls.radiusKm.value).toBe(-1);
    expect(component.hasSecondaryDraftChanges).toBeTrue();
  });

  it('deduplicates sort changes and removes default sorting from the URL and applied snapshot', () => {
    component.filterForm.controls.sortBy.setValue('ratingDesc'); component.applySort();
    expect(component.hasDraftChanges).toBeFalse();
    expect(component.hasSecondaryDraftChanges).toBeFalse();
    const count = listingService.search.calls.count();
    component.applySort(); expect(listingService.search.calls.count()).toBe(count);
    component.filterForm.controls.sortBy.setValue('newest'); component.applySort();
    expect(component.detailParams['sortBy']).toBeUndefined();
    expect(listingService.search.calls.mostRecent().args[0].sortBy).toBeUndefined();
    expect(component.hasDraftChanges).toBeFalse();
  });

  it('renders applied-only chips with a distinct amenity count and catalog fallback', () => {
    component.filterForm.patchValue({ titleContains: 'Central', addressContains: 'Athens', minPrice: 0, maxPrice: 12.5, minRating: 4.5, amenityIds: [1, 2, 1], onlyAlwaysAvailable: true });
    component.applyFilters();
    component.filterForm.patchValue({ titleContains: 'Draft', minPrice: -1, amenityIds: [3] });
    fixture.detectChanges();
    expect(component.appliedFilterChips.map(chip => chip.label)).toEqual(['Title: Central', 'Address: Athens', 'Min: 0 €/h', 'Max: 12.5 €/h', 'Rating: 4.5+', 'Amenity #1', 'Amenity #2', 'Always available']);
    expect(fixture.nativeElement.querySelector('.applied-filters').textContent).toContain('8 applied filters');
    expect(fixture.nativeElement.querySelectorAll('.filter-chip').length).toBe(8);
    component.amenities = [{ id: 1, name: 'Covered' }];
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[aria-label="Remove Covered"]')).not.toBeNull();
  });

  it('removes just one scalar criterion and retains applied geography, period and unrelated invalid drafts', () => {
    (component as any).restoreNavigation(convertToParamMap({ lat: '38', lng: '24', radiusKm: '3', place: 'Athens', startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T12:00', minPrice: '4', maxPrice: '20', titleContains: 'Central' }));
    component.filterForm.patchValue({ titleContains: 'Draft', minPrice: 50, radiusKm: -1 });
    component.filterForm.controls.titleContains.markAsDirty();
    component.removeAppliedFilter(component.appliedFilterChips.find(chip => chip.key === 'maxPrice')!);
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ lat: 38, lng: 24, radiusKm: 3, startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T12:00', minPrice: 4, titleContains: 'Central' }));
    expect(component.detailParams['maxPrice']).toBeUndefined();
    expect(component.filterForm.controls.maxPrice.value).toBeNull();
    expect(component.filterForm.controls.minPrice.value).toBe(50);
    expect(component.filterForm.controls.radiusKm.value).toBe(-1);
    expect(component.filterForm.controls.titleContains.value).toBe('Draft');
    expect(component.filterForm.controls.titleContains.dirty).toBeTrue();
    expect(component.hasSecondaryDraftChanges).toBeTrue();
  });

  it('removes individual amenities from applied and draft selections without losing other IDs', () => {
    component.filterForm.controls.amenityIds.setValue([1, 2]); component.applyFilters();
    component.filterForm.controls.amenityIds.setValue([1, 3]);
    component.removeAppliedFilter(component.appliedFilterChips.find(chip => chip.amenityId === 1)!);
    expect(component.detailParams['amenityIds']).toEqual([2]);
    expect(component.filterForm.controls.amenityIds.value).toEqual([3]);
    expect(component.hasSecondaryDraftChanges).toBeTrue();
    component.removeAppliedFilter(component.appliedFilterChips[0]);
    expect(component.detailParams['amenityIds']).toBeUndefined();
    expect(component.filterForm.controls.amenityIds.value).toEqual([3]);
  });

  it('clears each scalar chip and rebases the applied snapshot without creating draft changes', () => {
    component.filterForm.patchValue({ titleContains: 'Central', addressContains: 'Athens', minPrice: 0, maxPrice: 20, minRating: 4, onlyAlwaysAvailable: true }); component.applyFilters();
    for (const chip of component.appliedFilterChips) {
      component.removeAppliedFilter(chip);
      expect(component.detailParams[chip.key]).toBeUndefined();
      expect(component.hasDraftChanges).toBeFalse();
    }
    expect(component.hasFilters).toBeFalse();
    expect(component.appliedFilterChips).toEqual([]);
  });

  it('preserves camera and shared map/list criteria while cancelling superseded sort and chip requests', () => {
    component.filterForm.controls.minPrice.setValue(4); component.applyFilters();
    component.showOnMap(component.listings[0]);
    const viewport = { latitude: 38, longitude: 24, zoom: 14 }; component.saveViewport(viewport);
    const oldList = new Subject<SearchListingsResult>(); const oldMap = new Subject<SearchListingItem[]>();
    listingService.search.and.returnValue(oldList); listingService.searchAll.and.returnValue(oldMap);
    component.filterForm.controls.sortBy.setValue('priceAsc'); component.applySort();
    expect(oldList.observed).toBeTrue(); expect(oldMap.observed).toBeTrue();
    expect(component.selectedMapListing).toBeNull(); expect(component.detailParams['selectedId']).toBeUndefined();
    expect(component.viewport).toEqual(viewport);
    listingService.search.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 12 })); listingService.searchAll.and.returnValue(of([]));
    component.removeAppliedFilter(component.appliedFilterChips[0]);
    expect(oldList.observed).toBeFalse(); expect(oldMap.observed).toBeFalse();
    expect(component.detailParams['mapZoom']).toBe(14);
    expect(listingService.searchAll.calls.mostRecent().args[0]).toEqual({ onlyActive: true, sortBy: 'priceAsc' });
    expect(listingService.search.calls.mostRecent().args[0]).toEqual({ onlyActive: true, sortBy: 'priceAsc', page: 1, pageSize: 12 });
    oldMap.next([{ id: 999 } as SearchListingItem]); expect(component.mapListings).toEqual([]);
  });

  it('restores sorting and chips and ignores their own URL echoes without overwriting drafts', () => {
    const route = TestBed.inject(ActivatedRoute);
    const params = new BehaviorSubject<ParamMap>(convertToParamMap({})); Object.defineProperty(route, 'queryParamMap', { value: params }); component.ngOnInit();
    params.next(convertToParamMap({ minPrice: '4', sortBy: 'ratingDesc', amenityIds: ['1', '2'] }));
    expect(component.filterForm.controls.sortBy.value).toBe('ratingDesc'); expect(component.appliedFilterChips.length).toBe(3);
    component.filterForm.controls.titleContains.setValue('Draft');
    component.filterForm.controls.sortBy.setValue('priceDesc'); component.applySort();
    const count = listingService.search.calls.count();
    params.next(convertToParamMap({ minPrice: '4', sortBy: 'priceDesc', amenityIds: ['1', '2'] }));
    expect(listingService.search.calls.count()).toBe(count); expect(component.filterForm.controls.titleContains.value).toBe('Draft');
    params.next(convertToParamMap({ minPrice: '8', sortBy: 'newest' }));
    expect(component.filterForm.controls.sortBy.value).toBe('newest'); expect(component.appliedFilterChips[0].label).toBe('Min: 8 €/h');
  });

  it('blocks sorting and chip removal for invalid saved criteria', () => {
    component.filterForm.controls.minPrice.setValue(4); component.applyFilters(); const chip = component.appliedFilterChips[0];
    (component as any).restoreNavigation(convertToParamMap({ minPrice: 'bad' }));
    const count = listingService.search.calls.count(); component.filterForm.controls.sortBy.setValue('priceAsc'); component.applySort(); component.removeAppliedFilter(chip);
    expect(listingService.search.calls.count()).toBe(count); expect(component.restorationError).toBeTruthy(); expect(component.appliedFilterChips).toEqual([]);
    fixture.detectChanges(); expect(fixture.nativeElement.querySelector('#filter-sort').matches(':disabled')).toBeTrue();
    component.resetFilters(); expect(component.restorationError).toBe(''); expect(component.filterForm.controls.sortBy.value).toBe('newest');
  });

  it('disables sorting on the initial invalid route and enables it after Reset', () => {
    fixture.destroy();
    const route = TestBed.inject(ActivatedRoute);
    spyOnProperty(route.snapshot, 'queryParamMap', 'get').and.returnValue(convertToParamMap({ minPrice: 'bad' }));
    fixture = TestBed.createComponent(ListingListComponent); component = fixture.componentInstance; fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#filter-sort').matches(':disabled')).toBeTrue();
    component.resetFilters(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#filter-sort').matches(':disabled')).toBeFalse();
  });

  it('retains malformed saved criteria without passing invalid values to date formatting', () => {
    (component as any).restoreNavigation(convertToParamMap({ startDateTime: 'bad', endDateTime: 'bad', minPrice: 'bad' }));
    expect(() => fixture.detectChanges()).not.toThrow();
    expect(component.draftDeparture).toBe(''); expect(component.restorationError).toBeTruthy();
  });

  it('corrects an expired saved period without applying secondary drafts typed afterward', fakeAsync(() => {
    (component as any).restoreNavigation(convertToParamMap({ startDateTime: '2020-01-15T10:00', endDateTime: '2020-01-15T12:00', minPrice: '4' }));
    expect(component.restorationError).toBeTruthy();
    component.filterForm.controls.minPrice.setValue(-1);
    component.editPeriod({ startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T12:00', incomplete: false }); tick(250);
    expect(component.restorationError).toBe('');
    expect(listingService.search.calls.mostRecent().args[0].minPrice).toBe(4);
    expect(component.filterForm.controls.minPrice.value).toBe(-1);
    expect(component.hasDraftChanges).toBeTrue();
  }));

  it('shows Apply feedback for secondary drafts rather than unresolved location typing', () => {
    component.filterForm.controls.where.setValue('Ath'); component.editPlace();
    expect(component.hasSecondaryDraftChanges).toBeFalse();
    component.filterForm.controls.minPrice.setValue(4);
    expect(component.hasSecondaryDraftChanges).toBeTrue();
  });

  it('automatically coalesces period changes and skips identical criteria', fakeAsync(() => {
    const period = { startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T11:00', incomplete: false };
    component.editPeriod(period); tick(200);
    component.editPeriod({ ...period, endDateTime: '2099-01-15T12:00' }); tick(249);
    expect(listingService.search).toHaveBeenCalledTimes(1);
    tick(1);
    expect(listingService.search.calls.mostRecent().args[0].endDateTime).toBe('2099-01-15T12:00');
    expect(component.hasDraftChanges).toBeFalse();
    component.editPeriod({ ...period, endDateTime: '2099-01-15T12:00' }); tick(250);
    expect(listingService.search).toHaveBeenCalledTimes(2);
  }));

  it('preserves applied secondary filters and invalid unsubmitted drafts during automatic updates', fakeAsync(() => {
    component.choosePlace({ label: 'Athens', latitude: 38, longitude: 24 });
    component.filterForm.patchValue({ minPrice: 10, maxPrice: 20, radiusKm: 3, amenityIds: [1] }); component.applyFilters();
    component.filterForm.patchValue({ minPrice: 100, radiusKm: -1, amenityIds: [2], titleContains: 'Draft only' });
    component.editPeriod({ startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T12:00', incomplete: false }); tick(250);
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ minPrice: 10, maxPrice: 20, radiusKm: 3, amenityIds: [1], endDateTime: '2099-01-15T12:00' }));
    expect(listingService.search.calls.mostRecent().args[0].titleContains).toBeUndefined();
    expect(component.filterForm.controls.minPrice.value).toBe(100);
    expect(component.filterForm.controls.radiusKm.value).toBe(-1);
    expect(component.hasDraftChanges).toBeTrue();
  }));

  it('keeps the previous search for incomplete primary edits and refreshes after clearing', fakeAsync(() => {
    component.editPeriod({ startDateTime: '2099-01-15T10:00', endDateTime: '', incomplete: true }); tick(250);
    expect(listingService.search).toHaveBeenCalledTimes(1);
    expect(component.periodError).toContain('length of stay');
    component.editPeriod({ startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T11:00', incomplete: false }); tick(250);
    component.editPeriod({ startDateTime: '', endDateTime: '', incomplete: false }); tick(250);
    expect(component.detailParams['startDateTime']).toBeUndefined();
    expect(component.hasDraftChanges).toBeFalse();
  }));

  it('debounces live suggestions, cancels superseded lookups and applies only a selected candidate', fakeAsync(() => {
    const old = new Subject<any>(), fresh = new Subject<any>();
    listingService.searchPlaces.calls.reset(); listingService.searchPlaces.and.returnValues(old, fresh);
    component.filterForm.controls.where.setValue('At'); component.typePlace(); tick(349);
    expect(listingService.searchPlaces).not.toHaveBeenCalled(); tick(1); expect(old.observed).toBeTrue();
    component.filterForm.controls.where.setValue('Athens'); component.typePlace(); expect(old.observed).toBeFalse();
    old.next({ items: [{ label: 'Old', latitude: 1, longitude: 1 }] }); tick(350);
    fresh.next({ items: [{ label: 'Athens', latitude: 38, longitude: 24 }] });
    expect(component.places[0].label).toBe('Athens'); expect(listingService.search).toHaveBeenCalledTimes(1);
    component.navigatePlaces(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    expect(component.activePlaceIndex).toBe(0);
    component.navigatePlaces(new KeyboardEvent('keydown', { key: 'Enter' })); tick(250);
    expect(component.detailParams['lat']).toBe(38); expect(component.placePanelOpen).toBeFalse();
    component.clearPlace(); tick(250); expect(component.detailParams['lat']).toBeUndefined();
  }));

  it('cancels pending suggestions on Escape, selection, reset and destruction', fakeAsync(() => {
    listingService.searchPlaces.calls.reset(); listingService.searchPlaces.and.returnValue(of({ items: [] }));
    component.filterForm.controls.where.setValue('Athens'); component.typePlace();
    component.navigatePlaces(new KeyboardEvent('keydown', { key: 'Escape' })); tick(350);
    expect(listingService.searchPlaces).not.toHaveBeenCalled();
    component.openPlaces(); tick(350); expect(listingService.searchPlaces).toHaveBeenCalledTimes(1);
    component.typePlace(); component.resetFilters(); tick(350); expect(listingService.searchPlaces).toHaveBeenCalledTimes(1);
    component.filterForm.controls.where.setValue('Athens'); component.typePlace(); fixture.destroy(); tick(350);
    expect(listingService.searchPlaces).toHaveBeenCalledTimes(1);
  }));

  it('automatically applies successful geolocation with the previously applied distance', fakeAsync(() => {
    component.filterForm.controls.radiusKm.setValue(12);
    component.useMyLocation();
    location.locate.calls.mostRecent().args[0]({ coords: { latitude: 38, longitude: 24 } }); tick(250);
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ lat: 38, lng: 24, radiusKm: 5 }));
    expect(component.filterForm.controls.radiusKm.value).toBe(12); expect(component.hasDraftChanges).toBeTrue();
  }));

  it('retains the bounds camera during an automatic period update without duplicate route requests', fakeAsync(() => {
    component.setViewMode('map');
    component.searchMapBounds({ south: 37, north: 39, west: 23, east: 25 });
    const camera = { latitude: 38, longitude: 24, zoom: 12 }; component.saveViewport(camera);
    component.editPeriod({ startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T11:00', incomplete: false }); tick(250);
    expect(component.viewport).toEqual(camera); expect(component.viewMode).toBe('map');
    expect(listingService.searchAll.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ south: 37, north: 39, startDateTime: '2099-01-15T10:00' }));
    const count = listingService.search.calls.count();
    (component as any).restoreNavigation(convertToParamMap(Object.fromEntries(Object.entries(component.detailParams).map(([key, value]) => [key, Array.isArray(value) ? value.map(String) : String(value)])))); tick(500);
    expect(listingService.search.calls.count()).toBe(count);
  }));

  it('keeps modal drafts without searching when dismissed', () => {
    const dialog = fixture.nativeElement.querySelector('.filter-panel') as HTMLDialogElement;
    spyOn(dialog, 'showModal');
    component.openFilters();
    expect(dialog.showModal).toHaveBeenCalled();
    const calls = listingService.search.calls.count();
    component.filterForm.controls.titleContains.setValue('Draft title');
    component.closeFilters();
    expect(component.filterForm.controls.titleContains.value).toBe('Draft title');
    expect(component.hasDraftChanges).toBeTrue();
    expect(listingService.search.calls.count()).toBe(calls);
  });
  it('keeps invalid filters open and dismisses only a valid submission', () => {
    spyOn(component, 'closeFilters');
    component.filterForm.patchValue({ minPrice: 20, maxPrice: 10 });
    component.applyFilters();
    expect(component.closeFilters).not.toHaveBeenCalled();
    component.filterForm.controls.maxPrice.setValue(30);
    component.applyFilters();
    expect(component.closeFilters).toHaveBeenCalled();
    expect(component.hasDraftChanges).toBeFalse();
    expect(listingService.search.calls.mostRecent().args[0].minPrice).toBe(20);
  });
  it('resets draft and applied criteria together', () => {
    component.filterForm.controls.titleContains.setValue('Draft title');
    component.resetFilters();
    expect(component.filterForm.controls.titleContains.value).toBe('');
    expect(component.hasDraftChanges).toBeFalse();
    expect(component.hasFilters).toBeFalse();
  });

  it('marks a same-label geographic change as an unapplied draft', () => {
    component.choosePlace({ label: 'Current location', latitude: 44.43, longitude: 26.1 });
    component.applyFilters();
    expect(component.hasDraftChanges).toBeFalse();
    component.choosePlace({ label: 'Current location', latitude: 44.44, longitude: 26.1 });
    expect(component.hasDraftChanges).toBeTrue();
  });

  it('keeps empty API errors visible for list and map retry', () => {
    const error = new HttpErrorResponse({ status: 503, error: [] });
    listingService.search.and.returnValue(throwError(() => error));
    component.loadPage(1);
    expect(component.errorMessages.length).toBeGreaterThan(0);
    listingService.searchAll.and.returnValue(throwError(() => error));
    component.setViewMode('map');
    expect(component.mapErrorMessages.length).toBeGreaterThan(0);
  });

  /** Completes the most recent controlled browser location request. */
  const locateSuccess = (latitude = 44.43, longitude = 26.1): void =>
    location.locate.calls.mostRecent().args[0]({ coords: { latitude, longitude } });

  it('selects location only as a draft and applies it with retained filters', () => {
    expect(location.locate).not.toHaveBeenCalled();
    component.filterForm.patchValue({ titleContains: 'Central', radiusKm: 3, amenityIds: [1], sortBy: 'priceAsc' });
    const calls = listingService.search.calls.count();
    const params = { ...component.detailParams };
    component.useMyLocation();
    component.useMyLocation();
    expect(location.locate).toHaveBeenCalledTimes(1);
    locateSuccess();
    expect(component.isLocationLoading).toBeFalse();
    expect(component.selectedPlace?.label).toBe('Current location');
    expect(component.searchArea).toBeNull();
    expect(component.detailParams).toEqual(params);
    expect(listingService.search.calls.count()).toBe(calls);
    component.applyFilters();
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
      lat: 44.43, lng: 26.1, radiusKm: 3, titleContains: 'Central', amenityIds: [1], sortBy: 'priceAsc'
    }));
  });

  for (const [code, message] of [[1, 'denied'], [2, 'unavailable'], [3, 'timed out']] as const) {
    it(`preserves an existing centre/results on location failure ${code} and allows retry`, () => {
      component.choosePlace({ label: 'Existing', latitude: 10, longitude: 20 });
      component.applyFilters();
      const results = component.listings;
      component.useMyLocation();
      location.locate.calls.mostRecent().args[1]({ code });
      expect(component.locationError).toContain(message);
      expect(component.selectedPlace?.label).toBe('Existing');
      expect(component.searchArea?.label).toBe('Existing');
      expect(component.listings).toBe(results);
      component.useMyLocation();
      locateSuccess();
      expect(component.locationError).toBe('');
    });
  }

  for (const reason of ['not supported', 'requires a secure HTTPS connection']) {
    it(`reports capability failure: ${reason}`, () => {
      location.unavailableReason = reason;
      component.useMyLocation();
      expect(location.locate).not.toHaveBeenCalled();
      expect(component.locationError).toContain(reason);
      expect(component.isLocationLoading).toBeFalse();
    });
  }

  it('handles synchronous failure and invalid coordinates', () => {
    location.locate.and.throwError('unavailable');
    component.useMyLocation();
    expect(component.locationError).toContain('unavailable');
    location.locate.and.stub();
    for (const [lat, lng] of [[NaN, 0], [91, 0], [0, 181], [0, Infinity]]) {
      component.useMyLocation();
      locateSuccess(lat, lng);
      expect(component.selectedPlace).toBeNull();
      expect(component.locationError).toContain('unavailable');
    }
  });

  it('bounds permission waiting and ignores expired callbacks after retry', fakeAsync(() => {
    component.useMyLocation();
    const old = location.locate.calls.mostRecent().args;
    tick(15_000);
    expect(component.isLocationLoading).toBeFalse();
    expect(component.locationError).toContain('timed out');
    component.useMyLocation();
    old[0]({ coords: { latitude: 1, longitude: 1 } });
    old[1]({ code: 1 });
    expect(component.isLocationLoading).toBeTrue();
    locateSuccess();
    tick(15_000);
    expect(component.locationError).toBe('');
    expect(component.selectedPlace?.latitude).toBe(44.43);
  }));

  for (const action of ['edit', 'find', 'choose', 'map', 'search', 'reset', 'destroy', 'restore']) {
    it(`ignores stale location callbacks after ${action}`, fakeAsync(() => {
      component.useMyLocation();
      const pending = location.locate.calls.mostRecent().args;
      switch (action) {
        case 'edit': component.editPlace(); break;
        case 'find': component.findPlaces(); break;
        case 'choose': component.choosePlace({ label: 'New', latitude: 1, longitude: 2 }); break;
        case 'map': component.searchMapCentre({ latitude: 1, longitude: 2 }); break;
        case 'search': component.applyFilters(); break;
        case 'reset': component.resetFilters(); break;
        case 'destroy': component.ngOnDestroy(); break;
        case 'restore': (component as any).restoreNavigation(convertToParamMap({ lat: '3', lng: '4', radiusKm: '5' })); break;
      }
      const selected = component.selectedPlace;
      pending[0]({ coords: { latitude: 50, longitude: 60 } });
      pending[1]({ code: 1 });
      tick(15_000);
      expect(component.selectedPlace).toBe(selected);
      expect(component.locationError).toBe('');
      expect(component.isLocationLoading).toBeFalse();
    }));
  }

  it('cancels pending place lookup while keeping location through other filter/view changes', () => {
    const places = new Subject<any>();
    listingService.searchPlaces.and.returnValue(places);
    component.filterForm.controls.where.setValue('Athens');
    component.findPlaces();
    component.useMyLocation();
    expect(places.observed).toBeFalse();
    component.filterForm.controls.radiusKm.setValue(7);
    component.setViewMode('map');
    locateSuccess(-33.8, 151.2);
    expect(component.selectedPlace?.latitude).toBe(-33.8);
    expect(component.filterForm.controls.radiusKm.value).toBe(7);
    expect(component.searchArea).toBeNull();
  });

  it('loads and renders active listings', () => {
    expect(listingService.search).toHaveBeenCalledWith({ page: 1, pageSize: 12, onlyActive: true });
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Central parking space');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Scheduled availability');
  });

  it('loads the next page when results remain', () => {
    listingService.search.and.returnValue(of({ page: 2, pageSize: 12, totalCount: 13, items: [] }));

    component.loadPage(2);

    expect(listingService.search).toHaveBeenCalledWith({ page: 2, pageSize: 12, onlyActive: true });
    expect(component.page).toBe(2);
  });

  it('shows an empty state when there are no active listings', () => {
    component.listings = [];
    component.totalCount = 0;
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No active listings yet');
  });

  it('switches to the map, loads all active listings once, and caches the result', () => {
    expect(component.viewMode).toBe('list');

    component.setViewMode('map');
    fixture.detectChanges();

    expect(listingService.searchAll).toHaveBeenCalledOnceWith({ onlyActive: true });
    expect(component.isMapLoaded).toBeTrue();
    expect(component.mapListings.length).toBe(1);

    component.setViewMode('list');
    component.setViewMode('map');
    expect(listingService.searchAll).toHaveBeenCalledTimes(1);
  });

  it('shows a selected listing preview and closes it without leaving map view', () => {
    component.setViewMode('map');
    component.selectMapListing(component.mapListings[0]);
    fixture.detectChanges();

    const preview = (fixture.nativeElement as HTMLElement).querySelector('.map-preview');
    expect(preview?.textContent).toContain('Central parking space');
    expect(preview?.textContent).toContain('View space');

    component.closeMapPreview();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.map-preview')).toBeNull();
    expect(component.viewMode).toBe('map');
  });

  it('keeps list results and supports retry when map loading fails', () => {
    listingService.searchAll.and.returnValues(
      throwError(() => new HttpErrorResponse({ status: 503, error: ['Map search is unavailable.'] })),
      of([])
    );

    component.setViewMode('map');
    fixture.detectChanges();
    expect(component.mapErrorMessages).toEqual(['Map search is unavailable.']);
    expect(component.listings.length).toBe(1);

    component.retryMap();
    expect(listingService.searchAll).toHaveBeenCalledTimes(2);
    expect(component.isMapLoaded).toBeTrue();
  });

  it('opens a card on the map and links cards beyond the paged list', () => {
    const extra = { ...component.listings[0], id: 99, title: 'Another mapped space' };
    listingService.searchAll.and.returnValue(of([component.listings[0], extra]));
    component.showOnMap(component.listings[0]);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('.map-result-list .listing-card').length).toBe(2);
    component.selectMapListing(extra);
    fixture.detectChanges();
    expect(element.querySelector('.selected-card')?.getAttribute('data-card-id')).toBe('99');
    expect(element.querySelector('.selected-card button')?.getAttribute('aria-pressed')).toBe('true');
    component.applyFilters();
    fixture.detectChanges();
    expect(element.querySelector('.selected-card')).toBeNull();
  });

  it('surfaces API errors', () => {
    listingService.search.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: ['Search is temporarily unavailable.']
    })));

    component.loadPage(1);
    fixture.detectChanges();

    expect(component.errorMessages).toEqual(['Search is temporarily unavailable.']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Search is temporarily unavailable.');
  });

  it('applies trimmed combined filters, preserves zero, and keeps them through pagination', () => {
    component.filterForm.patchValue({ titleContains: ' Central ', addressContains: ' Street ', minPrice: 0, maxPrice: 20, onlyAlwaysAvailable: true });
    expect(listingService.search).toHaveBeenCalledTimes(1);
    component.applyFilters();
    const filters = { onlyActive: true, titleContains: 'Central', addressContains: 'Street', minPrice: 0, maxPrice: 20, onlyAlwaysAvailable: true };
    expect(listingService.search).toHaveBeenCalledWith({ ...filters, page: 1, pageSize: 12 });
    component.loadPage(2);
    expect(listingService.search).toHaveBeenCalledWith({ ...filters, page: 2, pageSize: 12 });
    component.setViewMode('map');
    expect(listingService.searchAll).toHaveBeenCalledWith(filters);
  });

  it('omits blank text and preserves a zero maximum price', () => {
    component.filterForm.patchValue({ titleContains: '  ', addressContains: ' ', maxPrice: 0 });
    component.applyFilters();
    expect(listingService.search).toHaveBeenCalledWith({ onlyActive: true, maxPrice: 0, page: 1, pageSize: 12 });
  });

  it('rejects invalid filters without replacing results', () => {
    for (const values of [
      { minPrice: -1 }, { maxPrice: -1 }, { minPrice: 20, maxPrice: 10 },
      { titleContains: 'x'.repeat(121) }, { addressContains: 'x'.repeat(201) }
    ]) {
      component.filterForm.reset();
      component.filterForm.patchValue(values);
      component.applyFilters();
      expect(component.filterForm.invalid).toBeTrue();
      expect(component.listings.length).toBe(1);
    }
    expect(listingService.search).toHaveBeenCalledTimes(1);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Address must be at most 200 characters.');
  });

  it('invalidates map results and preview on Search and Reset while preserving the view', () => {
    component.setViewMode('map');
    component.selectMapListing(component.mapListings[0]);
    component.filterForm.patchValue({ titleContains: 'Central' });
    component.applyFilters();
    expect(component.selectedMapListing).toBeNull();
    expect(listingService.searchAll).toHaveBeenCalledWith({ onlyActive: true, titleContains: 'Central' });
    component.resetFilters();
    expect(component.viewMode).toBe('map');
    expect(component.hasFilters).toBeFalse();
    expect(component.filterForm.controls.minPrice.value).toBeNull();
    expect(listingService.searchAll).toHaveBeenCalledTimes(3);
    expect(listingService.searchAll.calls.mostRecent().args).toEqual([{ onlyActive: true }]);
    expect(listingService.search.calls.mostRecent().args).toEqual([{ onlyActive: true, page: 1, pageSize: 12 }]);
  });

  it('invalidates a hidden map without eagerly reloading it', () => {
    component.setViewMode('map');
    component.setViewMode('list');
    component.applyFilters();
    expect(component.isMapLoaded).toBeFalse();
    expect(listingService.searchAll).toHaveBeenCalledTimes(1);
    component.setViewMode('map');
    expect(listingService.searchAll).toHaveBeenCalledTimes(2);
  });

  it('shows a filtered empty state in both views and retries with applied filters', () => {
    listingService.search.and.returnValue(of({ page: 1, pageSize: 12, totalCount: 0, items: [] }));
    listingService.searchAll.and.returnValues(throwError(() => new Error()), of([]));
    component.filterForm.patchValue({ titleContains: 'Missing' });
    component.applyFilters();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No spaces match your filters');
    component.setViewMode('map');
    component.filterForm.patchValue({ titleContains: 'Unsubmitted' });
    component.retryMap();
    expect(listingService.searchAll.calls.mostRecent().args).toEqual([{ onlyActive: true, titleContains: 'Missing' }]);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No spaces match your filters');
  });

  it('cancels superseded list and map requests and cleans up on destruction', () => {
    const oldList = new Subject<SearchListingsResult>();
    const newList = new Subject<SearchListingsResult>();
    const oldMap = new Subject<SearchListingItem[]>();
    const newMap = new Subject<SearchListingItem[]>();
    listingService.search.and.returnValues(oldList, newList);
    listingService.searchAll.and.returnValues(oldMap, newMap);
    component.loadPage(2);
    component.setViewMode('map');
    component.filterForm.patchValue({ titleContains: 'New' });
    component.applyFilters();
    expect(oldList.observed).toBeFalse();
    expect(oldMap.observed).toBeFalse();
    oldList.next({ page: 2, pageSize: 12, totalCount: 999, items: [] });
    oldMap.next([]);
    expect(component.totalCount).toBe(0);
    expect(component.page).toBe(1);
    expect(component.isLoading).toBeTrue();
    expect(component.isMapLoading).toBeTrue();
    expect(component.isMapLoaded).toBeFalse();
    fixture.destroy();
    expect(newList.observed).toBeFalse();
    expect(newMap.observed).toBeFalse();
  });
  it('keeps duration-only intent unapplied and clears it on reset', () => {
    const params = { ...component.detailParams };
    component.editPeriod({ startDateTime: '', endDateTime: '', incomplete: true });
    expect(component.hasDraftChanges).toBeTrue();
    component.applyFilters();
    expect(component.periodError).toContain('length of stay');
    expect(component.detailParams).toEqual(params);
    component.resetFilters();
    expect(component.periodError).toBeNull();
    expect(component.hasDraftChanges).toBeFalse();
  });

  it('shares the requested period between list, map and detail links', () => {
    const period = { startDateTime: '2099-01-15T10:00', endDateTime: '2099-01-15T12:00' };
    component.filterForm.patchValue(period);
    component.applyFilters();
    component.setViewMode('map');
    expect(listingService.searchAll).toHaveBeenCalledWith({ onlyActive: true, ...period });
    expect(component.detailParams).toEqual({ ...period, view: 'map' });
    component.resetFilters();
    expect(component.filterForm.controls.startDateTime.value).toBe('');
    expect(component.detailParams).toEqual({ view: 'map' });
  });

  it('rejects incomplete, past and ambiguous periods without replacing applied results', () => {
    for (const period of [
      { startDateTime: '2099-01-15T10:00', endDateTime: '' },
      { startDateTime: '2020-01-15T10:00', endDateTime: '2020-01-15T12:00' },
      { startDateTime: '2030-10-27T03:30', endDateTime: '2030-10-27T05:00' }
    ]) {
      component.filterForm.patchValue(period);
      component.applyFilters();
      expect(component.periodError).not.toBeNull();
      expect(component.listings.length).toBe(1);
    }
    expect(listingService.search).toHaveBeenCalledTimes(1);
  });

  it('requires an explicit resolved place and never substitutes address matching', () => {
    const place = { label: 'Syntagma, Athens', latitude: 37.9755, longitude: 23.7348 };
    listingService.searchPlaces.and.returnValue(of({ items: [place] }));
    component.filterForm.controls.where.setValue('Syntagma');
    component.applyFilters();
    expect(listingService.search).toHaveBeenCalledTimes(1);
    component.findPlaces();
    expect(listingService.searchPlaces).toHaveBeenCalledOnceWith('Syntagma');
    component.choosePlace(component.places[0]);
    component.applyFilters();
    expect(listingService.search).toHaveBeenCalledWith({ onlyActive: true, lat: 37.9755, lng: 23.7348, radiusKm: 5, page: 1, pageSize: 12 });
    expect(component.detailParams['place']).toBe('Syntagma, Athens');
  });

  it('cancels stale place lookups and permits map search after provider failure', () => {
    const old = new Subject<{ items: never[] }>();
    listingService.searchPlaces.and.returnValue(old);
    component.filterForm.controls.where.setValue('Athens');
    component.findPlaces();
    component.editPlace();
    expect(old.observed).toBeFalse();
    expect(component.isPlaceLoading).toBeFalse();
    listingService.searchPlaces.and.returnValue(throwError(() => new Error('provider unavailable')));
    component.findPlaces();
    expect(component.placeError).toContain('unavailable');
    component.searchMapCentre({ latitude: 37.98, longitude: 23.73 });
    expect(component.placeError).toBe('');
    expect(component.searchArea?.latitude).toBe(37.98);
  });


  it('shares secondary filters with map, pagination and detail, then resets', () => {
    component.filterForm.patchValue({ amenityIds: [2, 1, 2], minRating: 4.5, sortBy: 'priceAsc' });
    component.applyFilters();
    component.loadPage(2);
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ page: 2, amenityIds: [2, 1], minRating: 4.5, sortBy: 'priceAsc' }));
    expect(component.detailParams['amenityIds']).toEqual([2, 1]);
    component.setViewMode('map');
    expect(listingService.searchAll.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ amenityIds: [2, 1], minRating: 4.5, sortBy: 'priceAsc' }));
    component.resetFilters();
    expect(component.filterForm.controls.sortBy.value).toBe('newest');
    expect(listingService.search.calls.mostRecent().args[0]).toEqual({ page: 1, pageSize: 12, onlyActive: true });
  });

  it('restores repeated amenities and sorting without catalog availability', () => {
    const route = TestBed.inject(ActivatedRoute);
    spyOnProperty(route.snapshot, 'queryParamMap', 'get').and.returnValue(convertToParamMap({ amenityIds: ['2', '3', '2'], minRating: '4.5', sortBy: 'ratingDesc' }));
    listingService.getAmenities.and.returnValue(throwError(() => new Error('offline')));
    component.ngOnInit();
    expect(component.amenityError).toContain('could not be loaded');
    expect(component.filterForm.controls.amenityIds.value).toEqual([2, 3]);
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ amenityIds: [2, 3], minRating: 4.5, sortBy: 'ratingDesc' }));
    listingService.getAmenities.and.returnValue(of({ items: [{ id: 2, name: 'Lighting' }] }));
    component.loadAmenities();
    expect(component.amenityError).toBe('');
    expect(component.filterForm.controls.amenityIds.value).toEqual([2, 3]);
  });

  it('renders photos, review summary and period cost with a broken-image fallback', () => {
    component.listings[0] = { ...component.listings[0], coverPhotoUrl: 'api/listing/7/photos/1/content', averageRating: 4.5, ratingCount: 2, estimatedPeriodCost: 18.75 };
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('4.5 / 5');
    expect(element.textContent).toContain('Estimated period cost:');
    expect(element.textContent).toContain('18.75');
    element.querySelector('img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(element.querySelector('img')).toBeNull();
    expect(element.textContent).toContain('No photo available');
  });
  it('restores page, selection and camera without passing navigation fields to the API', () => {
    const route = TestBed.inject(ActivatedRoute);
    spyOnProperty(route.snapshot, 'queryParamMap', 'get').and.returnValue(convertToParamMap({
      view: 'map', page: '2', selectedId: '7', mapLat: '37.98', mapLng: '23.73', mapZoom: '14', amenityIds: ['2', '3']
    }));
    listingService.search.and.returnValue(of({ page: 2, pageSize: 12, totalCount: 13, items: [] }));
    component.ngOnInit();
    expect(component.page).toBe(2);
    expect(component.viewMode).toBe('map');
    expect(component.selectedMapListing?.id).toBe(7);
    expect(component.viewport).toEqual({ latitude: 37.98, longitude: 23.73, zoom: 14 });
    expect(listingService.search.calls.mostRecent().args[0]).toEqual({ onlyActive: true, amenityIds: [2, 3], page: 2, pageSize: 12 });
    expect(component.detailParams['selectedId']).toBe(7);
  });

  it('retries the requested page and bounds correction when results shrink', () => {
    listingService.search.and.returnValues(
      throwError(() => new Error('offline')),
      of({ page: 4, pageSize: 12, totalCount: 13, items: [] }),
      of({ page: 2, pageSize: 12, totalCount: 13, items: [] })
    );
    component.loadPage(4);
    expect(component.page).toBe(4);
    component.loadPage(component.page);
    expect(component.page).toBe(2);
    expect(listingService.search.calls.mostRecent().args[0].page).toBe(2);
    listingService.search.and.returnValue(of({ page: 2, pageSize: 12, totalCount: 0, items: [] }));
    component.loadPage(2);
    expect(component.page).toBe(1);
  });

  it('retains selection and viewport through map failure, but clears a missing successful result', () => {
    component.saveViewport({ latitude: 37.98, longitude: 23.73, zoom: 14 });
    listingService.searchAll.and.returnValues(throwError(() => new Error('offline')), of(component.listings), of([]));
    component.showOnMap(component.listings[0]);
    expect(component.detailParams['selectedId']).toBe(7);
    expect(component.detailParams['mapZoom']).toBe(14);
    component.retryMap();
    expect(component.selectedMapListing?.id).toBe(7);
    component.retryMap();
    expect(component.selectedMapListing).toBeNull();
    expect(component.detailParams['selectedId']).toBeUndefined();
  });

  it('handles mounted route changes without refetching unchanged criteria', () => {
    const route = TestBed.inject(ActivatedRoute);
    const params = new BehaviorSubject<ParamMap>(convertToParamMap({}));
    Object.defineProperty(route, 'queryParamMap', { value: params });
    component.ngOnInit();
    listingService.search.calls.reset();
    params.next(convertToParamMap({ view: 'map', selectedId: '7', mapLat: '38', mapLng: '24', mapZoom: '13' }));
    expect(component.viewMode).toBe('map');
    expect(component.selectedMapListing?.id).toBe(7);
    expect(listingService.search).not.toHaveBeenCalled();
    expect(listingService.searchAll).toHaveBeenCalledTimes(1);
    params.next(convertToParamMap({ view: 'list', mapLat: '38', mapLng: '24', mapZoom: '13' }));
    expect(component.viewMode).toBe('list');
    expect(listingService.search).not.toHaveBeenCalled();
    params.next(convertToParamMap({ titleContains: 'Changed' }));
    expect(listingService.search).toHaveBeenCalledTimes(1);
    fixture.destroy();
    expect(params.observed).toBeFalse();
  });

  it('does not broaden malformed or expired restored searches and recovers through Reset', () => {
    const route = TestBed.inject(ActivatedRoute);
    const params = spyOnProperty(route.snapshot, 'queryParamMap', 'get');
    for (const invalid of [{ minPrice: 'nope' }, { lat: '38' }, { startDateTime: '2020-01-01T10:00', endDateTime: '2020-01-01T11:00' }]) {
      params.and.returnValue(convertToParamMap(invalid));
      listingService.search.calls.reset();
      component.ngOnInit();
      fixture.detectChanges();
      expect(component.restorationError).toContain('invalid or expired');
      expect(listingService.search).not.toHaveBeenCalled();
      expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('No active listings yet');
    }
    component.resetFilters();
    expect(component.restorationError).toBe('');
    expect(listingService.search).toHaveBeenCalledTimes(1);
  });

  it('ignores its own URL echo without overwriting drafts or issuing extra requests', () => {
    const route = TestBed.inject(ActivatedRoute);
    const params = new BehaviorSubject<ParamMap>(convertToParamMap({}));
    Object.defineProperty(route, 'queryParamMap', { value: params });
    component.ngOnInit();
    component.filterForm.controls.titleContains.setValue('Unsubmitted');
    component.useMyLocation();
    component.setViewMode('map');
    const count = listingService.search.calls.count();
    params.next(convertToParamMap({ view: 'map' }));
    expect(component.filterForm.controls.titleContains.value).toBe('Unsubmitted');
    expect(component.isLocationLoading).toBeTrue();
    locateSuccess();
    expect(component.selectedPlace?.label).toBe('Current location');
    expect(listingService.search.calls.count()).toBe(count);
  });

  it('recovers when mounted navigation returns from invalid criteria to the previous valid URL', () => {
    const route = TestBed.inject(ActivatedRoute);
    const params = new BehaviorSubject<ParamMap>(convertToParamMap({}));
    Object.defineProperty(route, 'queryParamMap', { value: params });
    component.ngOnInit();
    params.next(convertToParamMap({ minPrice: 'invalid' }));
    expect(component.restorationError).not.toBe('');
    listingService.search.calls.reset();
    params.next(convertToParamMap({}));
    expect(component.restorationError).toBe('');
    expect(listingService.search).toHaveBeenCalledOnceWith({ onlyActive: true, page: 1, pageSize: 12 });
  });

  it('applies bounds consistently without changing the camera and switches back to radius', () => {
    component.setViewMode('map');
    const camera = { latitude: 44.43, longitude: 26.1, zoom: 13 };
    component.saveViewport(camera);
    const calls = listingService.search.calls.count();
    component.saveViewport({ ...camera, zoom: 14 });
    expect(listingService.search.calls.count()).toBe(calls);
    component.filterForm.patchValue({ titleContains: 'Central', minPrice: 5, sortBy: 'priceAsc' });
    const bounds = { south: 44, north: 45, west: 26, east: 27 };
    component.filterForm.controls.radiusKm.setValue(0);
    component.searchMapBounds(bounds);
    expect(component.filterForm.controls.radiusKm.disabled).toBeTrue();
    expect(listingService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ ...bounds, titleContains: 'Central', minPrice: 5, page: 1 }));
    expect(listingService.searchAll.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining(bounds));
    expect(component.viewport?.zoom).toBe(14);
    expect(component.detailParams['south']).toBe(44);
    expect(component.detailParams['radiusKm']).toBeUndefined();
    component.applyFilters();
    expect(component.viewport?.zoom).toBe(14);
    component.choosePlace({ latitude: 38, longitude: 24, label: 'Athens' });
    expect(component.filterForm.controls.radiusKm.enabled).toBeTrue();
    component.filterForm.controls.radiusKm.setValue(5);
    component.applyFilters();
    expect(listingService.search.calls.mostRecent().args[0].south).toBeUndefined();
    expect(listingService.search.calls.mostRecent().args[0].lat).toBe(38);
    component.searchMapBounds(bounds);
    component.resetFilters();
    expect(listingService.search.calls.mostRecent().args[0].south).toBeUndefined();
  });

  it('restores bounds and rejects incomplete or conflicting saved geography', () => {
    const restore = (params: Record<string, string | undefined>) => (component as unknown as { restoreNavigation(params: ParamMap): void })
      .restoreNavigation(convertToParamMap(params));
    restore({ south: '44', north: '45', west: '26', east: '27' });
    expect(component.restorationError).toBe('');
    expect(listingService.search.calls.mostRecent().args[0].south).toBe(44);
    for (const params of [ { south: '44' }, { south: '45', north: '44', west: '26', east: '27' },
      { south: '44', north: '45', west: '26', east: '27', lat: '44', lng: '26', radiusKm: '5' } ]) {
      const calls = listingService.search.calls.count();
      restore(params);
      expect(component.restorationError).toContain('invalid');
      expect(listingService.search.calls.count()).toBe(calls);
    }
    restore({ south: '-10', north: '10', west: '170', east: '-170' });
    expect(component.restorationError).toBe('');
    expect(component.bounds?.west).toBe(170);
  });

});
