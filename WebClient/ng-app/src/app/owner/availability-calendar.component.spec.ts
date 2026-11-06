import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { BookingService } from '../api/bookings/booking.service';
import { AvailabilityCalendarComponent } from './availability-calendar.component';

describe('AvailabilityCalendarComponent', () => {
  let fixture: ComponentFixture<AvailabilityCalendarComponent>;
  let component: AvailabilityCalendarComponent;
  let service: jasmine.SpyObj<BookingService>;
  const booking = (id = 1, status = 1) => ({ bookingId: id, listingId: 3, renterId: 2, listingTitle: 'Space', startUtc: '2030-01-15T10:00:00', endUtc: '2030-01-15T12:00:00', canComplete: false, status, statusText: 'Confirmed', createdAt: '' });
  beforeEach(() => {
    service = jasmine.createSpyObj('BookingService', ['getForOwner']);
    service.getForOwner.and.returnValue(of({ page: 1, pageSize: 100, totalCount: 0, items: [] }));
    TestBed.configureTestingModule({ imports: [AvailabilityCalendarComponent], providers: [{ provide: BookingService, useValue: service }] });
    fixture = TestBed.createComponent(AvailabilityCalendarComponent); component = fixture.componentInstance;
    fixture.componentRef.setInput('listingId', 3); fixture.detectChanges();
  });
  it('uses Monday alignment, leap years and year boundaries', () => {
    component.month = '2024-02-01'; expect(component.days.filter(Boolean).length).toBe(29); expect(component.days[3]).toBe('2024-02-01');
    component.month = '2030-12-01'; component.moveMonth(1); expect(component.month).toBe('2031-01-01');
    component.moveMonth(-1); expect(component.selected).toBe('2030-12-01');
  });
  it('selects today in Romania even when the UTC date differs', () => {
    jasmine.clock().install();
    try { jasmine.clock().mockDate(new Date('2030-01-01T22:30:00Z')); component.today(); expect(component.selected).toBe('2030-01-02'); }
    finally { jasmine.clock().uninstall(); }
  });
  it('shows overlapping categories with exclusive midnight endings and multi-day windows', () => {
    component.windows = [{ availabilityWindowId: 1, listingId: 3, listingTitle: '', startDateTime: '2030-01-14T12:00:00', endDateTime: '2030-01-16T00:00:00.000', isBlocked: false, createdAt: '' }, { availabilityWindowId: 2, listingId: 3, listingTitle: '', startDateTime: '2030-01-15T14:00:00', endDateTime: '2030-01-15T15:00:00', isBlocked: true, createdAt: '' }];
    component.bookings = [booking()];
    expect(component.labels('2030-01-15')).toBe('Open, Confirmed, Blocked');
    expect(component.periods('2030-01-16')).toEqual([]);
    expect(component.periods('2030-01-14').length).toBe(1);
    component.unavailable = true; expect(component.periods('2030-01-15')).toEqual([]);
  });
  it('publishes only complete sequential pages and excludes other statuses', () => {
    const second = new Subject<any>();
    service.getForOwner.and.callFake(r => r.page === 1 ? of({ page: 1, pageSize: 100, totalCount: 101, items: [booking()] }) : second);
    component.loadBookings(); expect(component.bookings).toEqual([]); expect(component.loading).toBeTrue();
    second.next({ page: 2, pageSize: 100, totalCount: 101, items: [booking(2), booking(3, 0)] }); second.complete();
    expect(component.bookings.length).toBe(2); expect(component.loading).toBeFalse();
    expect(service.getForOwner).toHaveBeenCalledWith({ listingId: 3, statuses: [1], page: 2, pageSize: 100 });
  });
  it('discards incomplete results on later-page failure and supports retry', () => {
    service.getForOwner.and.callFake(r => r.page === 1 ? of({ page: 1, pageSize: 100, totalCount: 101, items: [booking()] }) : throwError(() => new Error('offline')));
    component.loadBookings(); expect(component.bookings).toEqual([]); expect(component.error).toContain('incomplete');
    service.getForOwner.and.returnValue(of({ page: 1, pageSize: 100, totalCount: 1, items: [booking()] }));
    component.loadBookings(); expect(component.error).toBe(''); expect(component.bookings.length).toBe(1);
  });
  it('retains navigation on snapshot replacement and cancels requests on destruction', () => {
    component.month = '2030-01-01'; component.selected = '2030-01-15';
    const pending = new Subject<any>(); service.getForOwner.and.returnValue(pending);
    fixture.componentRef.setInput('windows', []); fixture.detectChanges();
    expect(component.selected).toBe('2030-01-15'); expect(component.month).toBe('2030-01-01');
    expect(pending.observed).toBeTrue(); fixture.destroy(); expect(pending.observed).toBeFalse();
  });
  it('explains both baselines and uses labelled date buttons', () => {
    expect(fixture.nativeElement.textContent).toContain('only within published open windows');
    fixture.componentRef.setInput('alwaysAvailable', true); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Available by default');
    const day = fixture.nativeElement.querySelector('.day'); day.click(); fixture.detectChanges();
    expect(day.getAttribute('aria-pressed')).toBe('true');
  });
});

