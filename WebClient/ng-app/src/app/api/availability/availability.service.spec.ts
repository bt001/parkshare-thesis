import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AvailabilityService } from './availability.service';

describe('AvailabilityService', () => {
  let service: AvailabilityService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AvailabilityService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads future availability for a listing', () => {
    service.getForListing(8, '2026-07-13T10:00:00').subscribe();

    const request = http.expectOne((candidate) => candidate.url === 'api/availability/listing/8');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('startDateTime')).toBe('2026-07-13T10:00:00');
    request.flush({ listingId: 8, totalCount: 0, items: [] });
  });
});
