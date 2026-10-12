import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { RatingService } from '../api/ratings/rating.service';
import { ListingRatings } from '../api/ratings/rating.models';
import { ListingReviewsComponent } from './listing-reviews.component';

describe('ListingReviewsComponent', () => {
  const api = jasmine.createSpyObj<RatingService>('RatingService', ['get']);
  const result: ListingRatings = { page: 1, pageSize: 20, totalCount: 21, averageStars: 4.5,
    items: [{ id: 1, stars: 5, comment: '<b>Good</b>', createdAt: '2026-10-12T00:00:00Z' }] };
  beforeEach(() => {
    api.get.calls.reset(); api.get.and.returnValue(of(result));
    TestBed.configureTestingModule({ imports: [ListingReviewsComponent], providers: [{ provide: RatingService, useValue: api }] });
  });
  it('renders plain text with aggregate and retries the failed page independently', () => {
    const fixture = TestBed.createComponent(ListingReviewsComponent);
    fixture.componentRef.setInput('listingId', 7); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('4.5 / 5');
    expect(fixture.nativeElement.textContent).toContain('<b>Good</b>');
    expect(fixture.nativeElement.querySelector('b')).toBeNull();
    api.get.and.returnValue(throwError(() => new Error('offline')));
    fixture.componentInstance.load(2); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    api.get.and.returnValue(of({ ...result, page: 2 }));
    fixture.nativeElement.querySelector('button').click(); fixture.detectChanges();
    expect(api.get).toHaveBeenCalledWith(7, 2);
    expect(fixture.nativeElement.textContent).toContain('Page 2');
  });
  it('cancels stale listing requests and renders an empty state', () => {
    const stale = new Subject<ListingRatings>(); api.get.and.returnValue(stale);
    const fixture = TestBed.createComponent(ListingReviewsComponent);
    fixture.componentRef.setInput('listingId', 7); fixture.detectChanges();
    api.get.and.returnValue(of({ ...result, totalCount: 0, averageStars: null, items: [] }));
    fixture.componentRef.setInput('listingId', 8); fixture.detectChanges(); stale.next(result);
    expect(fixture.nativeElement.textContent).toContain('No reviews yet');
    expect(fixture.componentInstance.result?.totalCount).toBe(0);
    fixture.destroy(); expect(stale.observed).toBeFalse();
  });
});
