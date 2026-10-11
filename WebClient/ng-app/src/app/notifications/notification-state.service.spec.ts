import { DOCUMENT } from '@angular/common';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { AuthSession } from '../api/auth/auth.models';
import { testSession } from '../api/auth/auth-test.helpers';
import { NotificationService } from '../api/notifications/notification.service';
import { NotificationStateService } from './notification-state.service';

describe('NotificationStateService', () => {
  let state: NotificationStateService;
  let session: BehaviorSubject<AuthSession | null>;
  let api: jasmine.SpyObj<NotificationService>;
  let document: Document;
  beforeEach(() => {
    session = new BehaviorSubject<AuthSession | null>(testSession());
    api = jasmine.createSpyObj('NotificationService', ['unreadCount']);
    api.unreadCount.and.returnValue(of({ count: 4 }));
    TestBed.configureTestingModule({ providers: [NotificationStateService,
      { provide: NotificationService, useValue: api },
      { provide: AuthService, useValue: { session$: session, get session() { return session.value; } } }
    ] });
    state = TestBed.inject(NotificationStateService); document = TestBed.inject(DOCUMENT);
  });
  afterEach(() => state.ngOnDestroy());

  it('refreshes on entry, visible intervals and visibility return, then tears down', fakeAsync(() => {
    const hidden = spyOnProperty(document, 'hidden', 'get').and.returnValue(false);
    state.start(); state.start(); expect(api.unreadCount).toHaveBeenCalledTimes(1);
    tick(60_000); expect(api.unreadCount).toHaveBeenCalledTimes(2);
    hidden.and.returnValue(true); tick(60_000); expect(api.unreadCount).toHaveBeenCalledTimes(2);
    hidden.and.returnValue(false); document.dispatchEvent(new Event('visibilitychange'));
    expect(api.unreadCount).toHaveBeenCalledTimes(3);
    state.ngOnDestroy(); tick(60_000); document.dispatchEvent(new Event('visibilitychange'));
    expect(api.unreadCount).toHaveBeenCalledTimes(3);
  }));

  it('does not overlap polls and cancels old transport on explicit refresh', fakeAsync(() => {
    spyOnProperty(document, 'hidden', 'get').and.returnValue(false);
    const first = new Subject<{ count: number }>(); const second = new Subject<{ count: number }>();
    api.unreadCount.and.returnValues(first, second);
    state.start(); tick(120_000); expect(api.unreadCount).toHaveBeenCalledTimes(1);
    state.refresh(); expect(first.observed).toBeFalse(); expect(second.observed).toBeTrue();
    first.next({ count: 99 }); second.next({ count: 2 }); expect(state.count()).toBe(2);
    state.ngOnDestroy(); expect(second.observed).toBeFalse();
  }));

  it('clears old-account counts and cancels responses on account replacement and logout', () => {
    state.start(); expect(state.count()).toBe(4);
    const pending = new Subject<{ count: number }>(); api.unreadCount.and.returnValue(pending);
    session.next(testSession({ userId: 2 })); expect(state.count()).toBeNull();
    session.next(null); expect(pending.observed).toBeFalse();
    pending.next({ count: 99 }); expect(state.count()).toBeNull();
    const calls = api.unreadCount.calls.count(); state.refresh(); expect(api.unreadCount.calls.count()).toBe(calls);
  });

  it('keeps count stable across token refresh and marks failure unavailable until retry succeeds', () => {
    state.start(); session.next(testSession({ accessToken: 'renewed' }));
    expect(api.unreadCount).toHaveBeenCalledTimes(1);
    api.unreadCount.and.returnValue(throwError(() => new Error('offline'))); state.refresh();
    expect(state.count()).toBe(4); expect(state.unavailable()).toBeTrue();
    api.unreadCount.and.returnValue(of({ count: 0 })); state.refresh();
    expect(state.count()).toBe(0); expect(state.unavailable()).toBeFalse();
  });
});
