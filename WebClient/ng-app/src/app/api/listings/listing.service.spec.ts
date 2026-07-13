import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ListingService } from './listing.service';

describe('ListingService', () => {
  let service: ListingService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ListingService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches active listings with paging and omits unused filters', () => {
    service.search({ page: 2, pageSize: 12, onlyActive: true }).subscribe();

    const request = http.expectOne((candidate) => candidate.url === 'api/listing/search');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('12');
    expect(request.request.params.get('onlyActive')).toBe('true');
    expect(request.request.params.keys()).toEqual(['page', 'pageSize', 'onlyActive']);
    request.flush({ page: 2, pageSize: 12, totalCount: 0, items: [] });
  });

  it('loads a listing by id', () => {
    service.getById(42).subscribe();

    const request = http.expectOne('api/listing/42');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('supports owner listing reads and mutations', () => {
    const payload = { title: 'Space', description: 'Secure', pricePerHour: 10, latitude: 44, longitude: 26, address: null, size: 'Medium', isAlwaysAvailable: false };
    service.getOwnerListings().subscribe();
    expect(http.expectOne('api/listing/owner').request.method).toBe('GET');
    service.create(payload).subscribe();
    const create = http.expectOne('api/listing'); expect(create.request.method).toBe('POST'); expect(create.request.body).toEqual(payload);
    service.update(9, payload).subscribe();
    expect(http.expectOne('api/listing/9').request.method).toBe('PUT');
    service.activate(9).subscribe();
    const activate = http.expectOne('api/listing/activate'); expect(activate.request.method).toBe('PATCH'); expect(activate.request.body).toEqual({ id: 9 });
    service.deactivate(9).subscribe();
    expect(http.expectOne('api/listing/deactivate').request.method).toBe('PATCH');
  });
});
