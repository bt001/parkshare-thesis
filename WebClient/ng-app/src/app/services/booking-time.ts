/** Formats a UTC instant as a Romanian wall-clock input, regardless of browser location. */
export function romanianDateTime(instant = new Date()): string {
  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Europe/Bucharest', year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
  }).formatToParts(instant);
  const part = (type: string) => parts.find((item) => item.type === type)!.value;
  return `${part('year')}-${part('month')}-${part('day')}T${part('hour')}:${part('minute')}`;
}

/** Returns the next future minute in Romanian time. */
export function nextRomanianMinute(now = new Date()): string {
  return romanianDateTime(new Date((Math.floor(now.getTime() / 60_000) + 1) * 60_000));
}

/** Resolves modern Romanian wall time, rejecting DST gaps and repeated hours. */
function resolveWallTime(value: string): number | null {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)) return null;
  const wall = Date.parse(`${value}:00Z`);
  if (!Number.isFinite(wall)) return null;
  const candidates = [2, 3].map((hours) => wall - hours * 3_600_000)
    .filter((instant) => romanianDateTime(new Date(instant)) === value);
  return candidates.length === 1 ? candidates[0] : null;
}

/** Mirrors booking period validation; the API remains authoritative. */
export function bookingTimeError(start: string, end: string, now = new Date()): string | null {
  const startInstant = resolveWallTime(start);
  const endInstant = resolveWallTime(end);
  if (startInstant === null || endInstant === null) return 'Choose valid Romanian times outside the daylight-saving clock change.';
  if (startInstant >= endInstant) return 'End time must be after start time.';
  if (startInstant <= now.getTime()) return 'Booking start time must be in the future (Romanian time).';
  return null;
}
