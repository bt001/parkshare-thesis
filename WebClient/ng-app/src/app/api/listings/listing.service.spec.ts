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
});
