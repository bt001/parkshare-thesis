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
});
