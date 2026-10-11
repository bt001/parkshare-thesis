import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { AuthSession } from '../api/auth/auth.models';
import { testSession } from '../api/auth/auth-test.helpers';
import { ChatMessage, ChatPage } from '../api/chat/chat.models';
import { ChatService } from '../api/chat/chat.service';
import { BookingChatComponent } from './booking-chat.component';

describe('BookingChatComponent', () => {
  let fixture: ComponentFixture<BookingChatComponent>;
  let component: BookingChatComponent;
  let api: jasmine.SpyObj<ChatService>;
  let session: BehaviorSubject<AuthSession | null>;
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  const message: ChatMessage = { id: 7, bookingId: 9, senderId: 2, messageText: '<b>Hello</b>\nLine two', sentAt: '2026-10-12T10:00:00Z' };
  const result: ChatPage = { page: 1, pageSize: 50, totalCount: 51, ownerId: 2, renterId: 1,
    items: [message, { ...message, id: 6, senderId: 1, messageText: 'Earlier' }] };
  beforeEach(async () => {
    api = jasmine.createSpyObj('ChatService', ['get', 'send']);
    api.get.and.returnValue(of(result)); api.send.and.returnValue(of(message));
    session = new BehaviorSubject<AuthSession | null>(testSession());
    params = new BehaviorSubject(convertToParamMap({ id: '9' }));
    await TestBed.configureTestingModule({ imports: [BookingChatComponent], providers: [
      provideRouter([]), { provide: ActivatedRoute, useValue: { paramMap: params } },
      { provide: ChatService, useValue: api },
      { provide: AuthService, useValue: { session$: session } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(BookingChatComponent); component = fixture.componentInstance;
  });
  it('renders chronological escaped text, sender labels, paging and participant return links', () => {
    fixture.detectChanges();
    const articles = fixture.nativeElement.querySelectorAll('article');
    expect(articles[0].textContent).toContain('Earlier');
    expect(articles[1].textContent).toContain('<b>Hello</b>');
    expect(articles[1].querySelector('b')).toBeNull();
    expect(component.senderLabel(message)).toBe('Owner');
    expect(component.senderLabel(result.items[1])).toBe('You');
    component.load(2); expect(api.get).toHaveBeenCalledWith(9, 2);
    session.next(testSession({ userId: 2 })); fixture.detectChanges();
    expect(component.backLink).toBe('/owner/bookings');
    expect(component.senderLabel(result.items[1])).toBe('Renter');
  });
  it('bounds drafts, suppresses duplicate clicks and refreshes newest after success', () => {
    fixture.detectChanges();
    for (const value of ['', '  ', 'x'.repeat(2001)]) {
      component.draft.setValue(value); component.send();
    }
    expect(api.send).not.toHaveBeenCalled();
    const pending = new Subject<ChatMessage>(); api.send.and.returnValue(pending);
    component.draft.setValue('  Hello  '); component.send(); component.send();
    expect(api.send).toHaveBeenCalledOnceWith(9, 'Hello');
    expect(component.sending).toBeTrue();
    pending.next(message); pending.complete();
    expect(component.draft.value).toBe('');
    expect(api.get.calls.mostRecent().args).toEqual([9, 1]);
  });
  it('retains a failed draft without automatic resubmission', () => {
    fixture.detectChanges();
    api.send.and.returnValue(throwError(() => new Error('offline')));
    component.draft.setValue('Keep me'); component.send();
    expect(component.draft.value).toBe('Keep me'); expect(component.sendError).toContain('Refresh before resending');
    component.refresh(); expect(api.send).toHaveBeenCalledTimes(1);
  });
  it('shows loading, failed retrieval, retry and empty state', () => {
    const pending = new Subject<ChatPage>(); api.get.and.returnValue(pending);
    fixture.detectChanges(); expect(component.loading).toBeTrue();
    pending.error({ status: 503 }); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('No messages');
    api.get.and.returnValue(of({ ...result, totalCount: 0, items: [] })); component.refresh(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No messages');
  });
  it('cancels stale reads and sends on booking and account changes', () => {
    const oldRead = new Subject<ChatPage>(); api.get.and.returnValue(oldRead);
    fixture.detectChanges(); expect(oldRead.observed).toBeTrue();
    api.get.and.returnValue(of(result)); params.next(convertToParamMap({ id: '10' }));
    expect(oldRead.observed).toBeFalse(); expect(api.get.calls.mostRecent().args).toEqual([10, 1]);
    const oldSend = new Subject<ChatMessage>(); api.send.and.returnValue(oldSend);
    component.draft.setValue('Private'); component.send();
    session.next(null);
    expect(oldSend.observed).toBeFalse(); expect(component.items).toEqual([]);
    expect(component.draft.value).toBe(''); expect(component.accessible).toBeFalse();
    expect(component.sending).toBeFalse();
  });
  it('keeps inaccessible bookings read-only and cancels on destruction', () => {
    api.get.and.returnValue(throwError(() => ({ status: 404 })));
    fixture.detectChanges(); expect(component.error).toBe('Booking not found.');
    component.draft.setValue('denied'); component.send(); expect(api.send).not.toHaveBeenCalled();
    const pending = new Subject<ChatPage>(); api.get.and.returnValue(pending);
    component.refresh(); fixture.destroy(); expect(pending.observed).toBeFalse();
  });
});
