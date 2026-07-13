import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AvailabilityService } from '../api/availability/availability.service';
import { ListingService } from '../api/listings/listing.service';
import { OwnerAvailabilityComponent } from './owner-availability.component';

describe('OwnerAvailabilityComponent', () => {
  let component: OwnerAvailabilityComponent;
  let fixture: ComponentFixture<OwnerAvailabilityComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['getById']);
  const availabilityService = jasmine.createSpyObj<AvailabilityService>('AvailabilityService', ['getForOwner', 'create', 'update', 'delete']);

  beforeEach(async () => {
    listingService.getById.and.returnValue(of({ id: 3, ownerId: 1, title: 'Scheduled', description: 'Space', latitude: 44, longitude: 26, pricePerHour: 10, isActive: true, isAlwaysAvailable: false, createdAt: '2026-07-13' }));
    availabilityService.getForOwner.and.returnValue(of({ listingId: 3, totalCount: 0, items: [] }));
    const window = { availabilityWindowId: 4, listingId: 3, listingTitle: 'Scheduled', startDateTime: '2026-07-15T10:00', endDateTime: '2026-07-15T12:00', isBlocked: false, createdAt: '2026-07-13' };
    availabilityService.create.and.returnValue(of(window));
    availabilityService.update.and.returnValue(of(window));
    availabilityService.delete.and.returnValue(of({ availabilityWindowId: 4 }));
    await TestBed.configureTestingModule({ imports: [OwnerAvailabilityComponent], providers: [provideRouter([]), { provide: ListingService, useValue: listingService }, { provide: AvailabilityService, useValue: availabilityService }, { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '3' } } } }] }).compileComponents();
    fixture = TestBed.createComponent(OwnerAvailabilityComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('prevents invalid ranges and creates valid open windows', () => {
    component.form.setValue({ startDateTime: '2026-07-15T12:00', endDateTime: '2026-07-15T10:00', isBlocked: false, note: '' });
    component.submit(); expect(availabilityService.create).not.toHaveBeenCalled();
    component.form.patchValue({ endDateTime: '2026-07-15T14:00' }); component.submit();
    expect(availabilityService.create).toHaveBeenCalledWith(jasmine.objectContaining({ listingId: 3, isBlocked: false }));
  });
});
