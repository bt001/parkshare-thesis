import { BookingService } from '../api/bookings/booking.service';
import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';

import { AvailabilityService } from '../api/availability/availability.service';
import { ListingService } from '../api/listings/listing.service';
import { OwnerAvailabilityComponent } from './owner-availability.component';

describe('OwnerAvailabilityComponent', () => {
  let component: OwnerAvailabilityComponent;
  let fixture: ComponentFixture<OwnerAvailabilityComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['getById']);
  const availabilityService = jasmine.createSpyObj<AvailabilityService>('AvailabilityService', ['getForOwner', 'create', 'update', 'delete']);

  beforeEach(async () => {
    availabilityService.create.calls.reset();
    listingService.getById.and.returnValue(of({ amenities: [], id: 3, ownerId: 1, title: 'Scheduled', description: 'Space', latitude: 44, longitude: 26, pricePerHour: 10, isActive: true, isAlwaysAvailable: false, createdAt: '2026-07-13' }));
    availabilityService.getForOwner.and.returnValue(of({ listingId: 3, totalCount: 0, items: [] }));
    const window = { availabilityWindowId: 4, listingId: 3, listingTitle: 'Scheduled', startDateTime: '2026-07-15T10:00', endDateTime: '2026-07-15T12:00', isBlocked: false, createdAt: '2026-07-13' };
    availabilityService.create.and.returnValue(of(window));
    availabilityService.update.and.returnValue(of(window));
    availabilityService.delete.and.returnValue(of({ availabilityWindowId: 4 }));
    await TestBed.configureTestingModule({ imports: [OwnerAvailabilityComponent], providers: [provideRouter([]), { provide: BookingService, useValue: { getForOwner: () => of({ page: 1, pageSize: 100, totalCount: 0, items: [] }) } }, { provide: ListingService, useValue: listingService }, { provide: AvailabilityService, useValue: availabilityService }, { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '3' } } } }] }).compileComponents();
    fixture = TestBed.createComponent(OwnerAvailabilityComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('prevents invalid ranges and creates valid open windows', () => {
    component.form.setValue({ startDateTime: '2026-07-15T12:00', endDateTime: '2026-07-15T10:00', isBlocked: false, note: '' });
    component.submit(); expect(availabilityService.create).not.toHaveBeenCalled();
    component.form.patchValue({ endDateTime: '2026-07-15T14:00' }); component.submit();
    expect(availabilityService.create).toHaveBeenCalledWith(jasmine.objectContaining({ listingId: 3, isBlocked: false }));
  });

  it('retains drafts across save failure and retrieval retry', () => {
    component.form.setValue({ startDateTime: '2030-01-01T10:00', endDateTime: '2030-01-01T12:00', isBlocked: false, note: 'Keep draft' });
    availabilityService.create.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503, error: [] })));
    component.submit(); expect(component.errorMessages).toEqual(['The availability window could not be saved.']);
    availabilityService.getForOwner.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503, error: [] })));
    component.load(); fixture.detectChanges(); expect(fixture.nativeElement.textContent).not.toContain('No availability windows');
    availabilityService.getForOwner.and.returnValue(of({ listingId: 3, totalCount: 0, items: [] })); component.load();
    expect(component.form.controls.note.value).toBe('Keep draft'); expect(component.loadErrors).toEqual([]);
  });
  it('blocks duplicate deletion and cancellation while pending', () => {
    const pending = new Subject<any>(); availabilityService.delete.calls.reset(); availabilityService.delete.and.returnValue(pending);
    component.requestDelete(4); component.delete(4); component.delete(4); component.cancelDelete();
    expect(availabilityService.delete).toHaveBeenCalledTimes(1); expect(component.confirmingDeleteId).toBe(4); expect(component.isBusy).toBeTrue();
    pending.error(new HttpErrorResponse({ status: 503, error: [] })); expect(component.isBusy).toBeFalse();
    expect(component.errorMessages).toEqual(['The availability window could not be deleted.']);
  });
  it('shows required dates and note length feedback', () => {
    component.form.controls.note.setValue('x'.repeat(501)); component.submit(); fixture.detectChanges();
    for (const id of ['window-start-error', 'window-end-error', 'window-note-error']) expect(fixture.nativeElement.querySelector('#' + id)).not.toBeNull();
  });

});
