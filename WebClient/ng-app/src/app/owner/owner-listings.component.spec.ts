import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';

import { ListingService } from '../api/listings/listing.service';
import { OwnerListingsComponent } from './owner-listings.component';

describe('OwnerListingsComponent', () => {
  let component: OwnerListingsComponent;
  let fixture: ComponentFixture<OwnerListingsComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['getOwnerListings', 'activate', 'deactivate']);

  beforeEach(async () => {
    listingService.getOwnerListings.and.returnValue(of({ items: [{ id: 1, title: 'Owner space', address: 'Main St', pricePerHour: 10, latitude: 44, longitude: 26, isActive: true, createdAt: '2026-07-13' }] }));
    listingService.activate.and.returnValue(of({ listingId: 1, isActive: true }));
    listingService.deactivate.and.returnValue(of({ listingId: 1, isActive: false }));
    await TestBed.configureTestingModule({ imports: [OwnerListingsComponent], providers: [provideRouter([]), { provide: ListingService, useValue: listingService }] }).compileComponents();
    fixture = TestBed.createComponent(OwnerListingsComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('loads owner listings and toggles publishing state', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Owner space');
    component.setActive(component.listings[0], false);
    expect(listingService.deactivate).toHaveBeenCalledWith(1);
    expect(component.listings[0].isActive).toBeFalse();
  });

  it('surfaces status errors', () => {
    listingService.activate.and.returnValue(throwError(() => new Error('failed')));
    component.setActive(component.listings[0], true);
    expect(component.errorMessages.length).toBe(1);
  });

  it('distinguishes retrieval failure from empty results and retries', () => {
    listingService.getOwnerListings.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503, error: [] })));
    component.load(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Your listings could not be loaded.');
    expect(fixture.nativeElement.textContent).not.toContain('No listings yet');
    listingService.getOwnerListings.and.returnValue(of({ items: [] }));
    component.load(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No listings yet');
  });
  it('blocks duplicate publishing requests', () => {
    const pending = new Subject<any>(); listingService.deactivate.calls.reset();
    listingService.deactivate.and.returnValue(pending);
    component.setActive(component.listings[0], false); component.setActive(component.listings[0], false);
    expect(listingService.deactivate).toHaveBeenCalledTimes(1);
    pending.error(new HttpErrorResponse({ status: 503, error: [] }));
    expect(component.errorMessages).toEqual(['The listing status could not be changed.']);
  });

});
