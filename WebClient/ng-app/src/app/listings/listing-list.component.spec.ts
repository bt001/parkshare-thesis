import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { SearchListingItem, SearchListingsResult } from '../api/listings/listing.models';

import { ListingService } from '../api/listings/listing.service';
import { ListingListComponent } from './listing-list.component';

describe('ListingListComponent', () => {
  let component: ListingListComponent;
  let fixture: ComponentFixture<ListingListComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['search', 'searchAll', 'searchPlaces', 'getAmenities']);

  beforeEach(async () => {
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
        { provide: ListingService, useValue: listingService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ListingListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
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
    expect(component.detailParams).toEqual(period);
    component.resetFilters();
    expect(component.filterForm.controls.startDateTime.value).toBe('');
    expect(component.detailParams).toEqual({});
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
});
