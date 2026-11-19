import { bookingTimeError, nextAthensMinute, athensDateTime, departureForStay, stayMinutes } from './booking-time';

describe('Athens booking time', () => {
  const now = new Date('2026-09-12T12:00:00Z');
  it('formats Athens time and advances to the next future minute', () => {
    expect(athensDateTime(now)).toBe('2026-09-12T15:00');
    expect(nextAthensMinute(now)).toBe('2026-09-12T15:01');
    expect(athensDateTime(new Date('2026-01-12T12:00:00Z'))).toBe('2026-01-12T14:00');
  });
  it('rejects exact-now and past periods but accepts future periods', () => {
    expect(bookingTimeError('2026-09-12T15:00', '2026-09-12T16:00', now)).not.toBeNull();
    expect(bookingTimeError('2026-09-12T15:01', '2026-09-12T16:00', now)).toBeNull();
    expect(bookingTimeError('2020-01-01T10:00', '2020-01-01T11:00', now)).not.toBeNull();
  });
  it('rejects DST gaps and repeated hours while allowing valid crossings', () => {
    expect(bookingTimeError('2030-03-31T03:30', '2030-03-31T05:00', now)).not.toBeNull();
    expect(bookingTimeError('2030-10-27T03:30', '2030-10-27T05:00', now)).not.toBeNull();
    expect(bookingTimeError('2030-03-31T02:30', '2030-03-31T04:30', now)).toBeNull();
  });
});


describe('Stay period conversion', () => {
  it('adds half-hour and multi-day stays across midnight without the browser timezone', () => {
    expect(departureForStay('2099-01-15T23:45', 30)).toBe('2099-01-16T00:15');
    expect(departureForStay('2099-01-15T23:45', 1501)).toBe('2099-01-17T00:46');
    expect(stayMinutes('2099-01-15T23:45', '2099-01-17T00:46')).toBe(1501);
  });
  it('rejects invalid, fractional, empty and overflowing periods', () => {
    for (const length of [null, 0, -1, 1.5, Number.MAX_SAFE_INTEGER]) expect(departureForStay('2099-01-15T10:00', length)).toBe('');
    expect(departureForStay('2099-02-30T10:00', 60)).toBe('');
    expect(stayMinutes('bad', '2099-01-15T10:00')).toBeNull();
    expect(stayMinutes('2099-01-15T10:00', '2099-01-15T09:00')).toBeNull();
  });
  it('retains existing wall-clock DST validation', () => {
    const start = '2030-03-31T02:30';
    expect(bookingTimeError(start, departureForStay(start, 60), new Date('2026-01-01'))).not.toBeNull();
    expect(bookingTimeError(start, departureForStay(start, 120), new Date('2026-01-01'))).toBeNull();
  });
});
