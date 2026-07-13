import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { BookingStatus } from '../api/bookings/booking.models';
import { BookingService } from '../api/bookings/booking.service';
import { ListingService } from '../api/listings/listing.service';
import { OwnerBookingsComponent } from './owner-bookings.component';

describe('OwnerBookingsComponent', () => {
  let component: OwnerBookingsComponent;
  let fixture: ComponentFixture<OwnerBookingsComponent>;
  const bookingService = jasmine.createSpyObj<BookingService>('BookingService', ['getForOwner', 'approve', 'reject']);
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['getOwnerListings']);
  const pending = { bookingId: 8, listingId: 2, listingTitle: 'Space', renterId: 9, startUtc: '2026-07-15T10:00', endUtc: '2026-07-15T11:00', status: BookingStatus.Pending, statusText: 'Pending', createdAt: '2026-07-13' };

  beforeEach(async () => {
    listingService.getOwnerListings.and.returnValue(of({ items: [] }));
    bookingService.getForOwner.and.returnValue(of({ page: 1, pageSize: 20, totalCount: 1, items: [{ ...pending }] }));
    bookingService.approve.and.returnValue(of({ bookingId: 8, listingId: 2, renterId: 9, ownerId: 1, startDateTime: pending.startUtc, endDateTime: pending.endUtc, status: BookingStatus.Confirmed, statusText: 'Confirmed', createdAt: pending.createdAt }));
    bookingService.reject.and.returnValue(of({ bookingId: 8, listingId: 2, renterId: 9, ownerId: 1, startDateTime: pending.startUtc, endDateTime: pending.endUtc, status: BookingStatus.Rejected, statusText: 'Rejected', createdAt: pending.createdAt }));
    await TestBed.configureTestingModule({ imports: [OwnerBookingsComponent], providers: [provideRouter([]), { provide: BookingService, useValue: bookingService }, { provide: ListingService, useValue: listingService }] }).compileComponents();
    fixture = TestBed.createComponent(OwnerBookingsComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('approves pending bookings and hides further decisions', () => {
    component.decide(component.bookings[0], true);
    expect(bookingService.approve).toHaveBeenCalledWith(8);
    expect(component.bookings[0].status).toBe(BookingStatus.Confirmed);
  });
});
