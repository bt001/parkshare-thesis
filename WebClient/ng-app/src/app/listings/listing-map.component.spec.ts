import { PLATFORM_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchListingItem } from '../api/listings/listing.models';
import { ListingMapComponent } from './listing-map.component';

describe('ListingMapComponent', () => {
  const listing: SearchListingItem = {
    id: 7,
    title: '<Central parking>',
    address: '10 Main Street',
    latitude: 44.43,
    longitude: 26.1,
    pricePerHour: 12,
    isActive: true,
    isAlwaysAvailable: false,
    createdAt: '2026-07-13T08:00:00'
  };

  async function createComponent(platformId: object | string = 'browser'): Promise<{
    component: ListingMapComponent;
    fixture: ComponentFixture<ListingMapComponent>;
  }> {
    await TestBed.configureTestingModule({
      imports: [ListingMapComponent],
      providers: [{ provide: PLATFORM_ID, useValue: platformId }]
    }).compileComponents();

    const fixture = TestBed.createComponent(ListingMapComponent);
    const component = fixture.componentInstance;
    component.listings = [listing];
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return { component, fixture };
  }

  afterEach(() => TestBed.resetTestingModule());

  it('creates a safe price marker and emits the selected listing', async () => {
    const { component, fixture } = await createComponent();
    const selectionSpy = spyOn(component.listingSelected, 'emit');
    const marker = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('[data-listing-id="7"]');

    expect(marker).not.toBeNull();
    expect(marker?.textContent).toContain('12 RON');
    expect(marker?.innerHTML).not.toContain('<central parking>');
    marker?.closest<HTMLElement>('.leaflet-marker-icon')?.click();
    expect(selectionSpy).toHaveBeenCalledWith(listing);

    fixture.destroy();
  });

  it('does not initialize Leaflet during server rendering', async () => {
    const { fixture } = await createComponent('server');
    expect((fixture.nativeElement as HTMLElement).querySelector('.leaflet-container')).toBeNull();
    fixture.destroy();
  });
});
