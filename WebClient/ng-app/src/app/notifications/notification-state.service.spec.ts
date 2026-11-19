import { DOCUMENT } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { AuthSession } from '../api/auth/auth.models';
import { testSession } from '../api/auth/auth-test.helpers';
import { NotificationStateService } from './notification-state.service';

describe('NotificationStateService activity', () => {
  let state: NotificationStateService;
  let session: BehaviorSubject<AuthSession | null>;
  let http: HttpTestingController;
  let document: Document;
  let hidden: jasmine.Spy;
  const url = 'api/activity/summary';
  const counts = { unreadNotificationCount: 4, ownerAttentionCount: 2, renterAttentionCount: 1 };
  beforeEach(() => {
    session = new BehaviorSubject<AuthSession | null>(testSession());
    TestBed.configureTestingModule({ providers: [NotificationStateService, provideHttpClient(), provideHttpClientTesting(),
      { provide: AuthService, useValue: { session$: session, get session() { return session.value; } } }] });
    state = TestBed.inject(NotificationStateService); http = TestBed.inject(HttpTestingController); document = TestBed.inject(DOCUMENT);
    hidden = spyOnProperty(document, 'hidden', 'get').and.returnValue(false);
    spyOn(document, 'hasFocus').and.returnValue(true);
  });
  afterEach(() => { state.ngOnDestroy(); http.verify({ ignoreCancelled: true }); });
  it('polls every five seconds, pauses when hidden and never overlaps requests', fakeAsync(() => {
    state.start(); state.start(); const first = http.expectOne(url);
    tick(10000); http.expectNone(url); first.flush(counts);
    expect(state.ownerCount()).toBe(2); expect(state.renterCount()).toBe(1);
    tick(5000); http.expectOne(url).flush(counts);
    hidden.and.returnValue(true); tick(10000); http.expectNone(url);
    hidden.and.returnValue(false); document.dispatchEvent(new Event('visibilitychange')); http.expectOne(url).flush(counts);
    state.ngOnDestroy(); tick(5000); http.expectNone(url);
  }));
  it('clears counts and cancels private transport on account replacement and logout', () => {
    state.start(); http.expectOne(url).flush(counts); state.refresh(); const pending = http.expectOne(url);
    session.next(testSession({ userId: 2 })); expect(pending.cancelled).toBeTrue(); expect(state.count()).toBeNull();
    const next = http.expectOne(url); session.next(null); expect(next.cancelled).toBeTrue();
    expect(state.ownerCount()).toBeNull(); state.refresh(); http.expectNone(url);
  });
  it('retains counts after failure and recovers without a false zero', () => {
    state.start(); http.expectOne(url).flush(counts); session.next(testSession({ accessToken: 'new-token' })); http.expectNone(url);
    state.refresh(); http.expectOne(url).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(state.count()).toBe(4); expect(state.unavailable()).toBeTrue();
    state.refresh(); http.expectOne(url).flush({ ...counts, unreadNotificationCount: 0 });
    expect(state.count()).toBe(0); expect(state.unavailable()).toBeFalse();
  });
  it('acknowledges exact IDs only while focused, deduplicates success and retries failures', () => {
    state.start(); http.expectOne(url).flush(counts);
    (document.hasFocus as jasmine.Spy).and.returnValue(false);
    state.acknowledgeMessage(9, 7); http.expectNone('api/booking/9/messages/read');
    (document.hasFocus as jasmine.Spy).and.returnValue(true);
    state.acknowledgeMessage(9, 7); state.acknowledgeMessage(9, 7);
    const read = http.expectOne('api/booking/9/messages/read'); expect(read.request.body).toEqual({ messageIds: [7] });
    read.flush({}, { status: 503, statusText: 'Unavailable' });
    state.acknowledgeMessage(9, 7); http.expectOne('api/booking/9/messages/read').flush({ updatedCount: 1 });
    http.expectOne(url).flush(counts); state.acknowledgeMessage(9, 7); http.expectNone('api/booking/9/messages/read');
    state.acknowledgeNotifications([12]); http.expectOne('api/notification/12/read').flush({ isRead: true });
    http.expectOne(url).flush(counts);
  });
});
