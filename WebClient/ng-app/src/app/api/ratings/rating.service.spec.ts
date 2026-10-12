import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ANONYMOUS_API } from '../api-request';
import { RatingService } from './rating.service';

describe('RatingService', () => {
  let service: RatingService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(RatingService); http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('marks public paged reads as independent of authentication', () => {
    service.get(7, 2).subscribe();
    const request = http.expectOne('api/listing/7/ratings?page=2&pageSize=20');
    expect(request.request.context.get(ANONYMOUS_API)).toBeTrue();
    request.flush({ items: [], totalCount: 0, averageStars: null, page: 2, pageSize: 20 });
  });
  it('saves only review input without caller-owned identities', () => {
    service.save(9, 4, 'Good').subscribe();
    const request = http.expectOne('api/booking/9/rating');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ stars: 4, comment: 'Good' });
    expect(request.request.context.get(ANONYMOUS_API)).toBeFalse();
    request.flush({ id: 3, stars: 4, comment: 'Good', createdAt: '2026-10-12T00:00:00Z' });
  });
});
