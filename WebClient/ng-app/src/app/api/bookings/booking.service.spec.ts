import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { BookingService } from './booking.service';

describe('BookingService', () => {
  let service: BookingService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BookingService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('submits only the listing and requested time range', () => {
    const payload = {
      listingId: 5,
      startDateTime: '2026-07-14T09:00',
      endDateTime: '2026-07-14T11:00'
    };

    service.create(payload).subscribe();

    const request = http.expectOne('api/booking/bookingrequest');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    expect(request.request.body.renterId).toBeUndefined();
    request.flush({});
  });

  it('supports filtered owner review and decisions', () => {
    service.getForOwner({ listingId: 5, statuses: [0, 2], page: 2, pageSize: 20 }).subscribe();
    const owner = http.expectOne((candidate) => candidate.url === 'api/booking/owner');
    expect(owner.request.params.get('listingId')).toBe('5');
    expect(owner.request.params.getAll('statuses')).toEqual(['0', '2']);
    expect(owner.request.params.get('page')).toBe('2');
    service.approve(12).subscribe();
    const approve = http.expectOne('api/booking/approvebooking'); expect(approve.request.method).toBe('PATCH'); expect(approve.request.body).toEqual({ bookingId: 12 });
    service.reject(13).subscribe();
    const reject = http.expectOne('api/booking/rejectbooking'); expect(reject.request.method).toBe('PATCH'); expect(reject.request.body).toEqual({ bookingId: 13 });
  });
});
