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

  it('keeps modal drafts without searching when dismissed', () => {
    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
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
