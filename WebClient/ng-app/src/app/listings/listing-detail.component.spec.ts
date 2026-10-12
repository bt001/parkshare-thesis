import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { AvailabilityService } from '../api/availability/availability.service';
import { BookingService } from '../api/bookings/booking.service';
import { ListingService } from '../api/listings/listing.service';
import { ListingDetailComponent } from './listing-detail.component';

describe('ListingDetailComponent', () => {
  let component: ListingDetailComponent;
  let fixture: ComponentFixture<ListingDetailComponent>;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['getById']);
  const availabilityService = jasmine.createSpyObj<AvailabilityService>('AvailabilityService', ['getForListing']);
  const bookingService = jasmine.createSpyObj<BookingService>('BookingService', ['create']);
  const authService = jasmine.createSpyObj<AuthService>('AuthService', [], {
    session: {
      userId: 1,
      userName: 'renter',
      email: 'renter@example.com',
      role: 'Renter',
      name: 'Renter User',
      accessToken: 'token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      tokenType: 'Bearer',
      refreshToken: 'refresh',
      refreshTokenExpiresAt: '2099-01-02T00:00:00Z'
    }
  });

  const listing = {
    amenities: [{ id: 1, name: 'Lighting' }],
    id: 10,
    ownerId: 2,
    title: 'Always-open city space',
    description: 'A secure space close to the center.',
    address: '1 Center Road',
    latitude: 44.43,
    longitude: 26.1,
    size: 'Medium',
    pricePerHour: 10,
    isActive: true,
    isAlwaysAvailable: true,
    createdAt: '2026-07-13T08:00:00'
  };

  const openWindow = {
    availabilityWindowId: 4,
    listingId: 10,
    listingTitle: listing.title,
    startDateTime: '2099-07-14T09:00:00',
    endDateTime: '2099-07-14T11:00:00',
    isBlocked: false,
    note: null,
    createdAt: '2026-07-13T08:00:00'
  };

  const blockedWindow = {
    ...openWindow,
    availabilityWindowId: 5,
    startDateTime: '2099-07-15T09:00:00',
    endDateTime: '2099-07-15T11:00:00',
    isBlocked: true,
    note: 'Owner use'
  };

  beforeEach(async () => {
    listingService.getById.calls.reset();
    availabilityService.getForListing.calls.reset();
    bookingService.create.calls.reset();
    listingService.getById.and.returnValue(of(listing));
    availabilityService.getForListing.and.returnValue(of({
      listingId: 10,
      totalCount: 2,
      items: [openWindow, blockedWindow]
    }));
    bookingService.create.and.returnValue(of({
      bookingId: 99,
      listingId: 10,
      renterId: 1,
      ownerId: 2,
      startDateTime: openWindow.startDateTime,
      endDateTime: openWindow.endDateTime,
      status: 0,
      statusText: 'Pending',
      createdAt: '2026-07-13T09:00:00'
    }));

    await TestBed.configureTestingModule({
      imports: [ListingDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '10' }) } } },
        { provide: ListingService, useValue: listingService },
        { provide: AvailabilityService, useValue: availabilityService },
        { provide: BookingService, useValue: bookingService },
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ListingDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('displays saved amenities and an empty state', () => {
    expect(fixture.nativeElement.textContent).toContain('Lighting');
    component.listing = { ...listing, amenities: [] };
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No amenities listed.');
  });

  it('loads listing details and future availability together', () => {
    expect(listingService.getById).toHaveBeenCalledWith(10);
    expect(availabilityService.getForListing).toHaveBeenCalledWith(10, component.minimumDateTime);
    expect(component.openWindows).toEqual([openWindow]);
    expect(component.blockedWindows).toEqual([blockedWindow]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('generally available at any time');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Owner use');
  });

  it('prefills the form from an open window and submits a booking', () => {
    component.selectWindow(openWindow);
    component.submitBooking();
    fixture.detectChanges();

    expect(component.bookingForm.getRawValue()).toEqual({
      startDateTime: '2099-07-14T09:00',
      endDateTime: '2099-07-14T11:00'
    });
    expect(bookingService.create).toHaveBeenCalledWith({
      listingId: 10,
      startDateTime: '2099-07-14T09:00',
      endDateTime: '2099-07-14T11:00'
    });
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Request #99 sent');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Status: Pending');
    expect(fixture.nativeElement.querySelector('[role="status"] a[href="/bookings"]')?.textContent).toContain('My bookings');
  });

  it('does not submit an invalid time range', () => {
    component.bookingForm.setValue({
      startDateTime: '2099-07-14T11:00',
      endDateTime: '2099-07-14T09:00'
    });

    component.submitBooking();
    fixture.detectChanges();

    expect(bookingService.create).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('End time must be after start time');
  });

  it('prevents the owner from booking their own listing', () => {
    component.listing = { ...listing, ownerId: 1 };
    component.bookingForm.setValue({
      startDateTime: '2099-07-14T09:00',
      endDateTime: '2099-07-14T11:00'
    });

    component.submitBooking();
    fixture.detectChanges();

    expect(bookingService.create).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('You own this listing');
  });

  it('clips an ongoing window to the next future Romanian minute', () => {
    jasmine.clock().install(); jasmine.clock().mockDate(new Date('2026-09-12T12:00:30Z'));
    try {
      component.selectWindow({ ...openWindow, startDateTime: '2026-09-12T14:00:00', endDateTime: '2026-09-12T16:00:00' });
      expect(component.bookingForm.controls.startDateTime.value).toBe('2026-09-12T15:01');
      expect(component.canSelectWindow({ ...openWindow, endDateTime: '2026-09-12T15:00:50' })).toBeFalse();
    } finally { jasmine.clock().uninstall(); }
  });

  it('revalidates a period that expired while the form was open', () => {
    component.bookingForm.setValue({ startDateTime: '2020-01-01T10:00', endDateTime: '2020-01-01T11:00' });
    component.submitBooking();
    expect(bookingService.create).not.toHaveBeenCalled();
    expect(component.bookingErrorMessages[0]).toContain('future');
  });

  it('surfaces booking API validation errors', () => {
    bookingService.create.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: ['Requested time overlaps a blocked window.']
    })));
    component.selectWindow(openWindow);

    component.submitBooking();
    fixture.detectChanges();

    expect(component.bookingErrorMessages).toEqual(['Requested time overlaps a blocked window.']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Requested time overlaps a blocked window.');
  });

  it('surfaces listing or availability loading failures', () => {
    listingService.getById.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 404,
      error: ['Listing not found.']
    })));

    component.ngOnInit();
    fixture.detectChanges();

    expect(component.errorMessages).toEqual(['Listing not found.']);
  });
});

describe('ListingDetailComponent with an invalid route id', () => {
  it('rejects the route without calling APIs', async () => {
    const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['getById']);
    const availabilityService = jasmine.createSpyObj<AvailabilityService>('AvailabilityService', ['getForListing']);

    await TestBed.configureTestingModule({
      imports: [ListingDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'invalid' }) } } },
        { provide: ListingService, useValue: listingService },
        { provide: AvailabilityService, useValue: availabilityService },
        { provide: BookingService, useValue: jasmine.createSpyObj<BookingService>('BookingService', ['create']) },
        { provide: AuthService, useValue: { session: null } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(ListingDetailComponent);
    fixture.detectChanges();

    expect(listingService.getById).not.toHaveBeenCalled();
    expect(availabilityService.getForListing).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('This listing address is invalid.');
  });
});
