import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { BookingStatus, GetOwnerBookingsResult } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';
import { ListingService } from '../api/listings/listing.service';
import { OwnerBookingsComponent } from './owner-bookings.component';

describe('OwnerBookingsComponent', () => {
  let component: OwnerBookingsComponent;
  let fixture: ComponentFixture<OwnerBookingsComponent>;
  const service = jasmine.createSpyObj<BookingService>('BookingService', ['getForOwner', 'approve', 'reject', 'complete']);
  const listings = jasmine.createSpyObj<ListingService>('ListingService', ['getOwnerListings']);
  const pending = { canComplete: false, bookingId: 8, listingId: 2, listingTitle: 'Space', renterId: 9, startUtc: '2030-07-15T10:00', endUtc: '2030-07-15T11:00', status: BookingStatus.Pending, statusText: 'Pending', createdAt: '2026-07-13' };
  const page = (status = BookingStatus.Pending): GetOwnerBookingsResult => ({ page: 1, pageSize: 20, totalCount: 1, items: [{ ...pending, status }] });
  beforeEach(async () => {
    service.getForOwner.calls.reset(); service.approve.calls.reset();
    listings.getOwnerListings.and.returnValue(of({ items: [] }));
    service.getForOwner.and.returnValue(of(page()));
    service.approve.and.returnValue(of({ bookingId: 8, listingId: 2, renterId: 9, ownerId: 1, startDateTime: pending.startUtc, endDateTime: pending.endUtc, status: BookingStatus.Confirmed, statusText: 'Confirmed', createdAt: pending.createdAt }));
    await TestBed.configureTestingModule({ imports: [OwnerBookingsComponent], providers: [provideRouter([]), { provide: BookingService, useValue: service }, { provide: ListingService, useValue: listings }] }).compileComponents();
    fixture = TestBed.createComponent(OwnerBookingsComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('completes only eligible bookings and preserves successful completion through a reload failure', () => {
    service.complete.calls.reset();
    component.complete(pending); expect(service.complete).not.toHaveBeenCalled();
    service.complete.and.returnValue(of({ bookingId: 8 } as any));
    service.getForOwner.and.returnValue(throwError(() => new Error('offline')));
    component.complete({ ...pending, status: BookingStatus.Confirmed, canComplete: true });
    expect(service.complete).toHaveBeenCalledWith(8); expect(component.successMessage).toContain('completed');
    expect(component.errorMessages.length).toBeGreaterThan(0);
    component.refresh(); expect(service.complete).toHaveBeenCalledTimes(1);
  });

  it('prevents duplicate completion while the request is pending', () => {
    service.complete.calls.reset(); const pendingSave = new Subject<any>(); service.complete.and.returnValue(pendingSave);
    const eligible = { ...pending, status: BookingStatus.Confirmed, canComplete: true };
    component.complete(eligible); component.complete(eligible);
    expect(service.complete).toHaveBeenCalledTimes(1); fixture.destroy(); expect(pendingSave.observed).toBeFalse();
  });

  it('reloads all affected rows after approval', () => {
    service.getForOwner.and.returnValue(of({ ...page(BookingStatus.Confirmed), totalCount: 2, items: [{ ...pending, status: BookingStatus.Confirmed }, { ...pending, bookingId: 9, status: BookingStatus.Rejected }] }));
    component.decide(component.bookings[0], true);
    expect(component.bookings.map(b => b.status)).toEqual([BookingStatus.Confirmed, BookingStatus.Rejected]);
    expect(component.totalCount).toBe(2);
    expect(component.successMessage).toContain('approved');
  });

  it('retains applied Pending zero filters during refresh and paging', () => {
    component.filters.setValue({ listingId: '2', status: '0' }); component.applyFilters();
    component.filters.setValue({ listingId: '3', status: '1' }); component.refresh();
    expect(service.getForOwner.calls.mostRecent().args[0]).toEqual({ page: 1, pageSize: 20, listingId: 2, statuses: [0] });
  });

  it('falls back to the last page after a filtered decision removes rows', () => {
    component.page = 2;
    service.getForOwner.and.returnValues(of({ page: 2, pageSize: 20, totalCount: 20, items: [] }), of({ ...page(), totalCount: 20 }));
    component.decide(component.bookings[0], true);
    expect(service.getForOwner.calls.mostRecent().args[0].page).toBe(1);
    expect(component.page).toBe(1);
  });

  it('keeps decision success when reload fails and retries only retrieval', () => {
    service.getForOwner.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    component.decide(component.bookings[0], true);
    expect(component.successMessage).toContain('approved'); expect(component.errorMessages.length).toBe(1);
    service.getForOwner.and.returnValue(of(page(BookingStatus.Confirmed))); component.refresh();
    expect(service.approve).toHaveBeenCalledTimes(1); expect(component.errorMessages).toEqual([]);
  });

  it('cancels previous requests and ignores late responses', () => {
    const old = new Subject<GetOwnerBookingsResult>(); service.getForOwner.and.returnValue(old); component.load();
    service.getForOwner.and.returnValue(of(page(BookingStatus.Rejected))); component.applyFilters();
    old.next(page()); old.complete();
    expect(component.bookings[0].status).toBe(BookingStatus.Rejected);
    expect(old.observed).toBeFalse();
  });

  it('shows a retryable error instead of an empty state for an empty API error array', () => {
    service.getForOwner.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503, error: [] })));
    component.refresh(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('could not be loaded');
    expect(fixture.nativeElement.textContent).not.toContain('No matching requests');
    service.getForOwner.and.returnValue(of(page()));
    component.refresh(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('article').length).toBe(1);
  });

  it('uses initial loading state, disables decisions, and cancels on destroy', () => {
    const waiting = new Subject<GetOwnerBookingsResult>(); service.getForOwner.and.returnValue(waiting); component.ngOnInit();
    expect(component.isLoading).toBeTrue(); component.decide(pending, true); expect(service.approve).not.toHaveBeenCalled();
    fixture.destroy(); expect(waiting.observed).toBeFalse();
  });
});
