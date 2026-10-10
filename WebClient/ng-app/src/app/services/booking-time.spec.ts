import { bookingTimeError, nextRomanianMinute, romanianDateTime } from './booking-time';

describe('Romanian booking time', () => {
  const now = new Date('2026-09-12T12:00:00Z');
  it('formats Romanian time and advances to the next future minute', () => {
    expect(romanianDateTime(now)).toBe('2026-09-12T15:00');
    expect(nextRomanianMinute(now)).toBe('2026-09-12T15:01');
    expect(romanianDateTime(new Date('2026-01-12T12:00:00Z'))).toBe('2026-01-12T14:00');
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
