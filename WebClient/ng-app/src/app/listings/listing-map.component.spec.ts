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

  it('links selection without recreating markers and supports Space activation', async () => {
    const { component, fixture } = await createComponent();
    const marker = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.leaflet-marker-icon')!;
    fixture.componentRef.setInput('selectedListingId', listing.id);
    fixture.detectChanges();
    expect(marker.classList.contains('selected-marker')).toBeTrue();
    expect(marker.getAttribute('aria-pressed')).toBe('true');
    expect(marker.getAttribute('aria-label')).toContain('<Central parking>');
    expect((fixture.nativeElement as HTMLElement).querySelector('.leaflet-marker-icon')).toBe(marker);
    const selection = spyOn(component.listingSelected, 'emit');
    marker.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', cancelable: true }));
    marker.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true }));
    expect(selection).toHaveBeenCalledWith(listing);
    fixture.componentRef.setInput('selectedListingId', null);
    fixture.detectChanges();
    expect(marker.getAttribute('aria-pressed')).toBe('false');
    fixture.destroy();
  });

  it('does not initialize Leaflet during server rendering', async () => {
    const { fixture } = await createComponent('server');
    expect((fixture.nativeElement as HTMLElement).querySelector('.leaflet-container')).toBeNull();
    fixture.destroy();
  });
  it('restores the camera after rendering and does not refit on result updates', async () => {
    const { component, fixture } = await createComponent();
    const camera = { latitude: 37.98, longitude: 23.73, zoom: 14 };
    const emitted = spyOn(component.viewportChanged, 'emit');
    fixture.componentRef.setInput('viewport', camera);
    fixture.componentRef.setInput('selectedListingId', listing.id);
    fixture.detectChanges();
    const map = (component as unknown as { map: import('leaflet').Map }).map;
    expect(map.getCenter().lat).toBeCloseTo(camera.latitude, 5);
    expect(map.getCenter().lng).toBeCloseTo(camera.longitude, 5);
    expect(map.getZoom()).toBe(14);
    fixture.componentRef.setInput('listings', [{ ...listing, pricePerHour: 20 }]);
    fixture.detectChanges();
    expect(map.getCenter().lat).toBeCloseTo(camera.latitude, 5);
    expect(map.getZoom()).toBe(14);
    expect(emitted).not.toHaveBeenCalled();
    map.getContainer().style.width = '501px';
    (component as unknown as { resizeMap(): void }).resizeMap();
    expect(map.getCenter().lng).toBeCloseTo(camera.longitude, 5);
    expect(emitted).not.toHaveBeenCalled();
    map.setView([38.1, 24.1], 13, { animate: false });
    expect(emitted).toHaveBeenCalledWith({ latitude: 38.1, longitude: 24.1, zoom: 13 });
    fixture.destroy();
  });

});
