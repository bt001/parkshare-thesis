import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ListingService } from '../api/listings/listing.service';
import { ListingListComponent } from './listing-list.component';

describe('ListingListComponent', () => {
  let component: ListingListComponent;
  let fixture: ComponentFixture<ListingListComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['search', 'searchAll']);

  beforeEach(async () => {
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
});
