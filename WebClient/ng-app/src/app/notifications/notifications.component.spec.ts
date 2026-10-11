import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { AuthSession } from '../api/auth/auth.models';
import { testSession } from '../api/auth/auth-test.helpers';
import { NotificationPage } from '../api/notifications/notification.models';
import { NotificationService } from '../api/notifications/notification.service';
import { NotificationStateService } from './notification-state.service';
import { NotificationsComponent } from './notifications.component';

describe('NotificationsComponent', () => {
  let fixture: ComponentFixture<NotificationsComponent>;
  let component: NotificationsComponent;
  let api: jasmine.SpyObj<NotificationService>;
  let state: jasmine.SpyObj<NotificationStateService>;
  let session: BehaviorSubject<AuthSession | null>;
  const result: NotificationPage = { page: 1, pageSize: 20, totalCount: 21,
    items: [{ id: 7, bookingId: 9, type: 'booking-requested', message: 'Booking #9 for Central parking was requested.', isRead: false, createdAt: '2026-10-10T10:00:00Z' }] };
  beforeEach(async () => {
    api = jasmine.createSpyObj('NotificationService', ['get', 'markRead']);
    state = jasmine.createSpyObj('NotificationStateService', ['refresh']);
    session = new BehaviorSubject<AuthSession | null>(testSession());
    api.get.and.returnValue(of(result)); api.markRead.and.returnValue(of({ isRead: true }));
    await TestBed.configureTestingModule({ imports: [NotificationsComponent], providers: [provideRouter([]),
      { provide: NotificationService, useValue: api }, { provide: NotificationStateService, useValue: state },
      { provide: AuthService, useValue: { session$: session, get session() { return session.value; } } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(NotificationsComponent); component = fixture.componentInstance;
  });
  it('renders events and booking-list navigation, filters and pages', () => {
    fixture.detectChanges(); expect(fixture.nativeElement.textContent).toContain(result.items[0].message);
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/owner/bookings');
    const unread = Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button')).find(b => b.textContent === 'Unread')!;
    unread.click(); expect(api.get).toHaveBeenCalledWith(1, true);
    component.load(2); expect(api.get).toHaveBeenCalledWith(2, true);
    expect(component.bookingLink({ ...result.items[0], type: 'booking-approved' })).toBe('/bookings');
    expect(component.bookingLink({ ...result.items[0], bookingId: null })).toBeNull();
  });
  it('shows loading, failure and retry without a false empty state', () => {
    const pending = new Subject<NotificationPage>(); api.get.and.returnValue(pending);
    fixture.detectChanges(); expect(fixture.nativeElement.textContent).toContain('Loading notifications');
    pending.error(new Error('offline')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('No notifications yet');
    api.get.and.returnValue(of({ ...result, totalCount: 0, items: [] })); component.refresh(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No notifications yet'); expect(state.refresh).toHaveBeenCalled();
    component.filter(true); fixture.detectChanges(); expect(fixture.nativeElement.textContent).toContain('No unread notifications');
  });
  it('reconciles the last unread page and badge after a read without optimistic false success', () => {
    fixture.detectChanges(); component.filter(true); component.load(2);
    const pending = new Subject<{ isRead: boolean }>(); api.markRead.and.returnValue(pending);
    component.markRead(result.items[0]); component.markRead(result.items[0]); expect(api.markRead).toHaveBeenCalledTimes(1);
    expect(component.items[0].isRead).toBeFalse();
    api.get.and.returnValues(of({ ...result, page: 2, totalCount: 20, items: [] }), of({ ...result, totalCount: 20 }));
    pending.next({ isRead: true }); pending.complete();
    expect(api.get.calls.mostRecent().args).toEqual([1, true]); expect(component.page).toBe(1); expect(state.refresh).toHaveBeenCalled();
  });
  it('keeps a failed read retryable', () => {
    fixture.detectChanges(); api.markRead.and.returnValue(throwError(() => new Error('offline')));
    component.markRead(result.items[0]); expect(component.readError).toContain('could not be marked'); expect(component.readingId).toBeNull();
    expect(component.items[0].isRead).toBeFalse();
    api.markRead.and.returnValue(of({ isRead: true })); component.markRead(result.items[0]); expect(component.readError).toBe('');
  });
  it('cancels superseded requests, mutations and account-specific data', () => {
    const first = new Subject<NotificationPage>(); const second = new Subject<NotificationPage>();
    api.get.and.returnValues(first, second); fixture.detectChanges(); component.refresh();
    expect(first.observed).toBeFalse(); first.next(result); expect(component.items).toEqual([]);
    const mutation = new Subject<{ isRead: boolean }>(); api.markRead.and.returnValue(mutation);
    component.markRead(result.items[0]); session.next(null);
    expect(second.observed).toBeFalse(); expect(mutation.observed).toBeFalse(); expect(component.items).toEqual([]);
    api.get.and.returnValue(of(result)); session.next(testSession({ userId: 2 })); expect(component.items.length).toBe(1);
    fixture.destroy(); expect(session.observed).toBeFalse();
  });
});
