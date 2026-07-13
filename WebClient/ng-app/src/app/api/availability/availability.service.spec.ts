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

  it('supports owner availability reads and mutations', () => {
    service.getForOwner({ listingId: 8, startDateTime: '2026-07-14T08:00' }).subscribe();
    const owner = http.expectOne((candidate) => candidate.url === 'api/availability/owner');
    expect(owner.request.params.get('listingId')).toBe('8');
    const payload = { listingId: 8, startDateTime: '2026-07-14T08:00', endDateTime: '2026-07-14T10:00', isBlocked: false, note: null };
    service.create(payload).subscribe();
    const create = http.expectOne('api/availability'); expect(create.request.method).toBe('POST'); expect(create.request.body).toEqual(payload);
    service.update(4, payload).subscribe();
    expect(http.expectOne('api/availability/4').request.method).toBe('PUT');
    service.delete(4).subscribe();
    expect(http.expectOne('api/availability/4').request.method).toBe('DELETE');
  });
});
