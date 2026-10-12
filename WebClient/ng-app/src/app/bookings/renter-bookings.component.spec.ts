import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../api/auth/auth.service';
import { RatingService } from '../api/ratings/rating.service';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';

import { BookingService } from '../api/bookings/booking.service';
import { GetRenterBookingsResult } from '../api/bookings/booking.models';
import { RenterBookingsComponent } from './renter-bookings.component';

 describe('RenterBookingsComponent', () => {
  let fixture: ComponentFixture<RenterBookingsComponent>;
  let component: RenterBookingsComponent;
  const service = jasmine.createSpyObj<BookingService>('BookingService', ['getForRenter']);
  const ratings = jasmine.createSpyObj<RatingService>('RatingService', ['save']);
  const session = new BehaviorSubject<{ userId: number } | null>({ userId: 1 });
  const result: GetRenterBookingsResult = {
    page: 1, pageSize: 20, totalCount: 21,
    items: [{ bookingId: 5, listingId: 3, listingTitle: 'Central parking', listingAddress: 'Test Street 1',
      startDateTime: '2026-10-01T10:00:00', endDateTime: '2026-10-01T12:00:00', status: 0,
      statusText: 'Pending', createdAt: '2026-09-12T09:00:00Z' }]
  };

  beforeEach(async () => {
    session.next({ userId: 1 }); ratings.save.calls.reset();
    service.getForRenter.calls.reset();
    service.getForRenter.and.returnValue(of(result));
    await TestBed.configureTestingModule({ imports: [RenterBookingsComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: { session$: session } }, { provide: RatingService, useValue: ratings }, { provide: BookingService, useValue: service }] }).compileComponents();
    fixture = TestBed.createComponent(RenterBookingsComponent);
    component = fixture.componentInstance;
  });

  it('retains failed review drafts, saves edits and cancels back to persisted content', () => {
    fixture.detectChanges();
    const booking = { ...component.bookings[0], status: 3, rating: { id: 9, stars: 3, comment: 'Original', createdAt: '2026-10-12T00:00:00Z' } };
    component.bookings = [booking]; component.editRating(booking);
    expect(component.review.getRawValue()).toEqual({ stars: 3, comment: 'Original' });
    component.review.setValue({ stars: 5, comment: ' Edited ' });
    ratings.save.and.returnValue(throwError(() => new Error('offline'))); component.saveRating(booking);
    expect(component.ratingError).toContain('retry'); expect(component.review.controls.comment.value).toBe(' Edited ');
    expect(booking.rating.comment).toBe('Original');
    ratings.save.and.returnValue(of({ ...booking.rating, stars: 5, comment: 'Edited' })); component.saveRating(booking);
    expect(ratings.save).toHaveBeenCalledWith(booking.bookingId, 5, 'Edited');
    expect(booking.rating.stars).toBe(5); expect(component.editingId).toBeUndefined();
    component.editRating(booking); component.review.controls.comment.setValue('Discard'); component.cancelRating(); component.editRating(booking);
    expect(component.review.controls.comment.value).toBe('Edited');
  });

  it('cancels saves and clears private drafts on account changes', () => {
    fixture.detectChanges();
    const booking = { ...component.bookings[0], status: 3 };
    component.editRating(booking); component.review.controls.comment.setValue('Private');
    const pending = new Subject<any>(); ratings.save.and.returnValue(pending); component.saveRating(booking);
    component.saveRating(booking); expect(ratings.save).toHaveBeenCalledTimes(1);
    session.next(null);
    expect(pending.observed).toBeFalse(); expect(component.review.controls.comment.value).toBe('');
    expect(component.bookings).toEqual([]); expect(component.saving).toBeFalse();
    expect(component.editingId).toBeUndefined();
  });

  it('rejects invalid stars and clears a draft when leaving the page', () => {
    fixture.detectChanges(); component.editRating({ ...component.bookings[0], status: 3 });
    component.review.controls.stars.setValue(1.5); expect(component.canSaveRating).toBeFalse();
    component.review.controls.stars.setValue(5); component.review.controls.comment.setValue('x'.repeat(1001));
    expect(component.canSaveRating).toBeFalse(); fixture.destroy();
    expect(component.review.controls.comment.value).toBe('');
  });

  it('loads all statuses and renders stored booking details', () => {
    fixture.detectChanges();
    expect(service.getForRenter).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
    const text = fixture.nativeElement.textContent;
    for (const value of ['Central parking', 'Test Street 1', 'Pending', 'Request #5']) expect(text).toContain(value);
    expect(component.canGoBack).toBeFalse();
    expect(component.canGoForward).toBeTrue();
  });

  it('applies Pending zero and resets pagination without applying later draft changes', () => {
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = '0'; select.dispatchEvent(new Event('change'));
    fixture.nativeElement.querySelector('button[type="submit"]').click();
    expect(service.getForRenter).toHaveBeenCalledWith({ page: 1, pageSize: 20, statuses: [0] });
    component.status.setValue('2');
    service.getForRenter.and.returnValue(of({ ...result, page: 2 }));
    component.loadPage(2); component.refresh();
    expect(service.getForRenter.calls.mostRecent().args[0]).toEqual({ page: 2, pageSize: 20, statuses: [0] });
    expect(component.canGoBack).toBeTrue(); expect(component.canGoForward).toBeFalse();
    component.status.setValue(''); component.applyFilters();
    expect(service.getForRenter.calls.mostRecent().args[0]).toEqual({ page: 1, pageSize: 20 });
  });

  it('shows loading and distinguishes empty history from filtered empty results', () => {
    const pending = new Subject<GetRenterBookingsResult>();
    service.getForRenter.and.returnValue(pending);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading your bookings');
    pending.next({ ...result, totalCount: 0, items: [] }); pending.complete(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No bookings yet');
    service.getForRenter.and.returnValue(of({ ...result, totalCount: 0, items: [] }));
    component.status.setValue('3'); component.applyFilters(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No matching bookings');
  });

  it('shows retrieval errors without an empty state and retries the failed page and applied filter', () => {
    fixture.detectChanges();
    component.status.setValue('1'); component.applyFilters();
    service.getForRenter.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    component.loadPage(2); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('No matching bookings');
    component.status.setValue('2');
    service.getForRenter.and.returnValue(of({ ...result, page: 2 }));
    const retry = Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button')).find(button => button.textContent?.trim() === 'Retry');
    retry?.click();
    expect(service.getForRenter.calls.mostRecent().args[0]).toEqual({ page: 2, pageSize: 20, statuses: [1] });
  });

  it('cancels superseded requests and ignores their results', () => {
    const first = new Subject<GetRenterBookingsResult>();
    const second = new Subject<GetRenterBookingsResult>();
    service.getForRenter.and.returnValues(first, second);
    fixture.detectChanges(); component.refresh();
    expect(first.observed).toBeFalse(); expect(second.observed).toBeTrue();
    first.next(result); expect(component.bookings).toEqual([]); expect(component.isLoading).toBeTrue();
    second.next(result); second.complete();
    expect(component.bookings).toEqual(result.items); expect(component.isLoading).toBeFalse();
  });

  it('cancels retrieval on destruction', () => {
    const pending = new Subject<GetRenterBookingsResult>();
    service.getForRenter.and.returnValue(pending); fixture.detectChanges(); fixture.destroy();
    expect(pending.observed).toBeFalse();
  });

  it('refreshes stored owner decisions and lets users return from an empty later page', () => {
    fixture.detectChanges();
    service.getForRenter.and.returnValue(of({ ...result, items: [{ ...result.items[0], status: 1, statusText: 'Confirmed' }] }));
    component.refresh(); fixture.detectChanges(); expect(fixture.nativeElement.textContent).toContain('Confirmed');
    service.getForRenter.and.returnValue(of({ ...result, page: 2, totalCount: 1, items: [] }));
    component.loadPage(2); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No bookings on this page'); expect(component.canGoBack).toBeTrue();
  });
});
