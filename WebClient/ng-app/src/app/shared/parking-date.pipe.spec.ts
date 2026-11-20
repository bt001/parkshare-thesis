import { AthensTimestampPipe, ParkingPeriodPipe } from './parking-date.pipe';

describe('Parking date presentation', () => {
  const period = new ParkingPeriodPipe();
  const event = new AthensTimestampPipe();
  it('keeps wall clocks and condenses same-day stays without seconds', () => {
    expect(period.transform('2026-11-27T10:00:00', '2026-11-27T12:00:00')).toBe('Fri, 27 Nov 2026 · 10:00–12:00');
  });
  it('retains both dates and years for overnight and year-crossing stays', () => {
    expect(period.transform('2026-12-31T23:30', '2027-01-01T01:15')).toBe('Thu, 31 Dec 2026 · 23:30 – Fri, 1 Jan 2027 · 01:15');
    expect(period.transform('2026-11-27T23:30', '2026-11-28T01:15')).toContain('Sat, 28 Nov 2026');
    expect(period.transform('invalid')).toBe('Time unavailable');
    expect(period.transform('2026-02-30T10:00')).toBe('Time unavailable');
  });
  it('converts event instants to Athens including seasonal offsets and persisted offset-free UTC', () => {
    expect(event.transform('2026-11-27T08:00:00Z')).toBe('27 Nov 2026, 10:00');
    expect(event.transform('2026-07-27T08:00:00Z')).toBe('27 Jul 2026, 11:00');
    expect(event.transform('2026-11-27T08:00:00')).toBe(event.transform('2026-11-27T08:00:00Z'));
    expect(event.transform('bad')).toBe('Time unavailable');
  });
});
